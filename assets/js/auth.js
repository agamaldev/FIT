// ============================================================================
//  MAKE ME FIT — وحدة المصادقة والبيانات (Firebase)
// ----------------------------------------------------------------------------
//  ملف ES module واحد يحتوي كل تعامل مع Firebase. يُحمّل في كل صفحة عبر:
//    <script type="module" src="assets/js/auth.js"></script>
//
//  يكشف جسرَين عالميَّين للكود القديم (jQuery + inline onclick):
//    window.FITAuth  — تسجيل الدخول/الخروج وحالة المستخدم
//    window.FITData  — قراءة/كتابة بيانات المستخدم في Firestore
// ============================================================================

import { firebaseConfig } from "./firebase-config.js";
import { initializeApp } from "https://www.gstatic.com/firebasejs/11.0.0/firebase-app.js";
import {
  getAuth, onAuthStateChanged, signOut,
  GoogleAuthProvider, OAuthProvider, signInWithPopup,
  createUserWithEmailAndPassword, signInWithEmailAndPassword,
  sendEmailVerification, sendPasswordResetEmail, updateProfile
} from "https://www.gstatic.com/firebasejs/11.0.0/firebase-auth.js";
import {
  getFirestore, doc, getDoc, setDoc, addDoc, deleteDoc,
  collection, getDocs, query, orderBy, serverTimestamp
} from "https://www.gstatic.com/firebasejs/11.0.0/firebase-firestore.js";
import {
  getStorage, ref, uploadBytes, getDownloadURL
} from "https://www.gstatic.com/firebasejs/11.0.0/firebase-storage.js";

// ---------------------------------------------------------------------------
//  تهيئة
// ---------------------------------------------------------------------------
const CONFIG_READY =
  !!firebaseConfig.apiKey && !String(firebaseConfig.apiKey).includes("REPLACE");

let app, auth, db, storage;
let currentUser = null;     // كائن المستخدم الحالي أو null
let authResolved = false;   // هل حُسمت حالة المصادقة الأولى؟
const userListeners = [];   // مستمعو تغيّر المستخدم

if (CONFIG_READY) {
  app  = initializeApp(firebaseConfig);
  auth = getAuth(app);
  db   = getFirestore(app);
  storage = getStorage(app);

  onAuthStateChanged(auth, async (user) => {
    if (user) {
      try { await ensureUserDoc(user); }
      catch (e) { console.warn("[FIT] ensureUserDoc:", e); }
    }
    notify(user);
  });
} else {
  console.warn(
    "[FIT] لم يتم إعداد Firebase بعد. الرجاء ملء assets/js/firebase-config.js"
  );
  whenReady(() => notify(null));
}

// تُستدعى عند حسم حالة المصادقة (أو عند غياب الإعداد)
function notify(user) {
  currentUser  = user || null;
  authResolved = true;
  renderNavAuthUI(currentUser);
  userListeners.forEach((cb) => { try { cb(currentUser); } catch (e) { console.error(e); } });
}

function whenReady(fn) {
  if (document.readyState !== "loading") fn();
  else document.addEventListener("DOMContentLoaded", fn);
}

// ينشئ مستند المستخدم عند أول دخول إن لم يكن موجوداً
async function ensureUserDoc(user) {
  const ref = doc(db, "users", user.uid);
  const snap = await getDoc(ref);
  if (!snap.exists()) {
    await setDoc(ref, {
      profile: {
        displayName: user.displayName || "",
        email:       user.email || "",
        photoURL:    user.photoURL || null,
        weight:      null,
        height:      null,
        goal:        null,
        activity:    null,
        updatedAt:   serverTimestamp()
      }
    });
  }
}

// ===========================================================================
//  واجهة المصادقة  window.FITAuth
// ===========================================================================
function ensureReady() {
  if (!CONFIG_READY) {
    alert("لم يتم إعداد Firebase بعد.\nالرجاء ملء assets/js/firebase-config.js بمفاتيح مشروعك.");
    throw new Error("Firebase config not set");
  }
}

async function signInGoogle() {
  ensureReady();
  const provider = new GoogleAuthProvider();
  return signInWithPopup(auth, provider);
}

async function signInMicrosoft() {
  ensureReady();
  const provider = new OAuthProvider("microsoft.com");
  provider.addScope("User.Read");
  return signInWithPopup(auth, provider);
}

async function registerEmail({ name, email, password }) {
  ensureReady();
  const cred = await createUserWithEmailAndPassword(auth, email, password);
  if (name) await updateProfile(cred.user, { displayName: name });
  try { await ensureUserDoc(cred.user); } catch (e) { console.warn(e); }
  try { await sendEmailVerification(cred.user); } catch (e) { console.warn("[FIT] verify email:", e); }
  return cred;
}

async function loginEmail({ email, password }) {
  ensureReady();
  return signInWithEmailAndPassword(auth, email, password);
}

async function resetPassword(email) {
  ensureReady();
  return sendPasswordResetEmail(auth, email);
}

async function resendVerification() {
  ensureReady();
  if (auth.currentUser) return sendEmailVerification(auth.currentUser);
}

async function logout() {
  if (auth) { try { await signOut(auth); } catch (e) { console.warn(e); } }
  location.href = "index.html";
}

// يحوّل رموز أخطاء Firebase إلى رسائل عربية
function errMessage(code) {
  const map = {
    "auth/invalid-email":          "البريد الإلكتروني غير صحيح.",
    "auth/user-disabled":          "تم تعطيل هذا الحساب.",
    "auth/user-not-found":         "لا يوجد حساب بهذا البريد.",
    "auth/wrong-password":         "كلمة المرور غير صحيحة.",
    "auth/invalid-credential":     "البريد أو كلمة المرور غير صحيحة.",
    "auth/email-already-in-use":   "هذا البريد مسجّل بالفعل. جرّب تسجيل الدخول.",
    "auth/weak-password":          "كلمة المرور ضعيفة (٦ أحرف على الأقل).",
    "auth/popup-closed-by-user":   "تم إغلاق نافذة الدخول قبل الإكمال.",
    "auth/popup-blocked":          "المتصفح منع النافذة المنبثقة. اسمح بها وحاول مجدداً.",
    "auth/cancelled-popup-request":"تم إلغاء الطلب. حاول مرة أخرى.",
    "auth/account-exists-with-different-credential":
      "يوجد حساب بنفس البريد بطريقة دخول مختلفة.",
    "auth/network-request-failed": "تعذّر الاتصال بالشبكة. تحقّق من اتصالك.",
    "auth/operation-not-allowed":  "طريقة الدخول هذه غير مفعّلة في إعدادات Firebase."
  };
  return map[code] || "حدث خطأ غير متوقع. حاول مرة أخرى.";
}

window.FITAuth = {
  get user() { return currentUser; },
  get ready() { return CONFIG_READY; },
  signInGoogle,
  signInMicrosoft,
  registerEmail,
  loginEmail,
  resetPassword,
  resendVerification,
  logout,
  errMessage,

  // يسجّل مستمعاً لتغيّر المستخدم؛ يُنادى فوراً إن كانت الحالة محسومة
  onUser(cb) {
    if (typeof cb !== "function") return;
    userListeners.push(cb);
    if (authResolved) cb(currentUser);
  },

  // حماية صفحة: ينفّذ cb عند الدخول، ويحوّل لصفحة الدخول إن لم يكن داخلاً
  requireAuth(cb, redirect = "auth.html") {
    this.onUser((user) => {
      if (user) { if (cb) cb(user); }
      else location.replace(redirect);
    });
  },

  // رفع صورة المستخدم إلى Storage وتحديثها في الحساب وملف Firestore
  async uploadAvatar(file) {
    ensureReady();
    const u = auth.currentUser;
    if (!u) throw new Error("يجب تسجيل الدخول أولاً");
    if (!file || !file.type || !file.type.startsWith("image/")) throw new Error("يجب اختيار ملف صورة");
    if (file.size > 2 * 1024 * 1024) throw new Error("حجم الصورة يجب أن يكون أقل من ٢ ميجابايت");
    const avatarRef = ref(storage, `avatars/${u.uid}`);
    await uploadBytes(avatarRef, file);
    const url = await getDownloadURL(avatarRef);
    await updateProfile(u, { photoURL: url });
    try {
      await setDoc(doc(db, "users", u.uid),
        { profile: { photoURL: url, updatedAt: serverTimestamp() } }, { merge: true });
    } catch (e) { console.warn("[FIT] save photoURL:", e); }
    renderNavAuthUI(currentUser);
    return url;
  },

  // إعادة رسم الواجهة (القائمة + صورة الملف) من حالة المستخدم الحالية
  refreshUI() { renderNavAuthUI(currentUser); }
};

// ===========================================================================
//  طبقة البيانات  window.FITData  (Firestore)
// ===========================================================================
function uid() { return currentUser ? currentUser.uid : null; }
function requireUser() {
  if (!CONFIG_READY) { ensureReady(); }
  if (!uid()) throw new Error("not signed in");
  return uid();
}
function userDoc() { return doc(db, "users", uid()); }

window.FITData = {
  // --- الملف الشخصي ---
  async getProfile() {
    if (!uid()) return null;
    const snap = await getDoc(userDoc());
    return snap.exists() ? (snap.data().profile || null) : null;
  },
  async saveProfile(partial) {
    requireUser();
    await setDoc(
      userDoc(),
      { profile: { ...partial, updatedAt: serverTimestamp() } },
      { merge: true }
    );
  },

  // --- التمارين المفضلة (مصفوفة محدودة) ---
  async getFavorites() {
    if (!uid()) return [];
    const snap = await getDoc(userDoc());
    return snap.exists() ? (snap.data().favorites || []) : [];
  },
  // يضيف أو يزيل حسب وجود المعرّف؛ يعيد true إذا أصبح مفضّلاً
  async toggleFavorite(fav) {
    requireUser();
    const list = await this.getFavorites();
    const idx = list.findIndex((f) => f.id === fav.id);
    let favorited;
    if (idx >= 0) { list.splice(idx, 1); favorited = false; }
    else {
      list.push({ ...fav, type: fav.type || "exercise", addedAt: new Date().toISOString() });
      favorited = true;
    }
    await setDoc(userDoc(), { favorites: list }, { merge: true });
    return favorited;
  },
  async removeFavorite(id) {
    requireUser();
    const list = (await this.getFavorites()).filter((f) => f.id !== id);
    await setDoc(userDoc(), { favorites: list }, { merge: true });
  },

  // --- سجل حاسبة السعرات (subcollection يكبر مع الوقت) ---
  async saveCalcResult(result) {
    requireUser();
    return addDoc(collection(db, "users", uid(), "calcHistory"), {
      ...result,
      createdAt: serverTimestamp()
    });
  },
  async getCalcHistory() {
    if (!uid()) return [];
    const q = query(
      collection(db, "users", uid(), "calcHistory"),
      orderBy("createdAt", "desc")
    );
    const snap = await getDocs(q);
    return snap.docs.map((d) => ({ id: d.id, ...d.data() }));
  },
  async deleteCalcResult(id) {
    requireUser();
    await deleteDoc(doc(db, "users", uid(), "calcHistory", id));
  },

  // --- خطط التغذية المحفوظة (مصفوفة محدودة) ---
  async getNutritionPlans() {
    if (!uid()) return [];
    const snap = await getDoc(userDoc());
    return snap.exists() ? (snap.data().nutritionPlans || []) : [];
  },
  async saveNutritionPlan(plan) {
    requireUser();
    const id = plan.id || `${plan.calId}-${plan.varId}`;
    const list = await this.getNutritionPlans();
    if (!list.some((p) => p.id === id)) {
      list.push({ ...plan, id, savedAt: new Date().toISOString() });
      await setDoc(userDoc(), { nutritionPlans: list }, { merge: true });
      return true;
    }
    return false; // محفوظة مسبقاً
  },
  async removeNutritionPlan(id) {
    requireUser();
    const list = (await this.getNutritionPlans()).filter((p) => p.id !== id);
    await setDoc(userDoc(), { nutritionPlans: list }, { merge: true });
  },

  // --- سجل الوزن والقياسات (subcollection يكبر مع الوقت) ---
  async addWeightEntry(entry) {
    requireUser();
    const { weight, waist, chest, arms, date } = entry || {};
    const w = Number(weight);
    if (!w || w < 10 || w > 500) throw new Error("الوزن غير صحيح (١٠–٥٠٠ كجم)");
    return addDoc(collection(db, "users", uid(), "weightLog"), {
      date: date || new Date().toLocaleDateString("en-CA"),
      weight: w,
      waist: waist ? Number(waist) : null,
      chest: chest ? Number(chest) : null,
      arms:  arms  ? Number(arms)  : null,
      createdAt: serverTimestamp()
    });
  },
  async getWeightLog() {
    if (!uid()) return [];
    const q = query(collection(db, "users", uid(), "weightLog"), orderBy("date", "desc"));
    const snap = await getDocs(q);
    return snap.docs.map((d) => ({ id: d.id, ...d.data() }));
  },
  async deleteWeightEntry(id) {
    requireUser();
    await deleteDoc(doc(db, "users", uid(), "weightLog", id));
  },

  // --- متابعة إنجاز التمارين + السلسلة (streak) ---
  async getCompletedDates() {
    if (!uid()) return [];
    const snap = await getDoc(userDoc());
    return snap.exists() ? (snap.data().completedDates || []) : [];
  },
  async markWorkoutToday() {
    requireUser();
    const today = new Date().toLocaleDateString("en-CA");
    const dates = await this.getCompletedDates();
    if (!dates.includes(today)) {
      dates.push(today);
      await setDoc(userDoc(), { completedDates: dates }, { merge: true });
    }
    return this._getStreakStats(dates);
  },
  async unmarkWorkoutToday() {
    requireUser();
    const today = new Date().toLocaleDateString("en-CA");
    const dates = (await this.getCompletedDates()).filter((d) => d !== today);
    await setDoc(userDoc(), { completedDates: dates }, { merge: true });
    return this._getStreakStats(dates);
  },
  // يحسب السلسلة الحالية وأطول سلسلة والإجمالي من مصفوفة تواريخ "YYYY-MM-DD"
  _getStreakStats(dates) {
    const uniq = Array.from(new Set(dates || []));
    const total = uniq.length;
    if (!total) return { streak: 0, longestStreak: 0, total: 0 };
    const toNum = (s) => { const p = String(s).split("-").map(Number); return Date.UTC(p[0], p[1] - 1, p[2]) / 86400000; };
    const nums = uniq.map(toNum).sort((a, b) => a - b);
    let longest = 1, run = 1;
    for (let i = 1; i < nums.length; i++) {
      if (nums[i] === nums[i - 1] + 1) { run++; longest = Math.max(longest, run); }
      else { run = 1; }
    }
    const todayNum = toNum(new Date().toLocaleDateString("en-CA"));
    const last = nums[nums.length - 1];
    let current = 0;
    if (last === todayNum || last === todayNum - 1) {
      current = 1;
      for (let i = nums.length - 1; i > 0; i--) {
        if (nums[i] === nums[i - 1] + 1) current++;
        else break;
      }
    }
    return { streak: current, longestStreak: longest, total };
  },

  // --- خطتي المخصصة (مصفوفة محدودة) ---
  async getPlan() {
    if (!uid()) return [];
    const snap = await getDoc(userDoc());
    return snap.exists() ? (snap.data().customPlan || []) : [];
  },
  async addToPlan(exercise) {
    requireUser();
    const list = await this.getPlan();
    if (!list.some((e) => e.id === exercise.id)) {
      list.push({
        id: exercise.id,
        nameAr: exercise.nameAr || "",
        nameEn: exercise.nameEn || "",
        tab: exercise.tab || "",
        sets: exercise.sets ?? 4,
        reps: exercise.reps ?? 10,
        order: list.length
      });
      await setDoc(userDoc(), { customPlan: list }, { merge: true });
      return true;
    }
    return false; // موجود مسبقاً
  },
  async updatePlan(list) {
    requireUser();
    await setDoc(userDoc(), { customPlan: list }, { merge: true });
  },
  async removeFromPlan(id) {
    requireUser();
    const list = (await this.getPlan()).filter((e) => e.id !== id);
    list.forEach((e, i) => { e.order = i; });
    await setDoc(userDoc(), { customPlan: list }, { merge: true });
  },
  async clearPlan() {
    requireUser();
    await setDoc(userDoc(), { customPlan: [] }, { merge: true });
  },

  // --- الصيام المتقطع: الصيام النشط (حقل) + السجل (subcollection) ---
  async getActiveFast() {
    if (!uid()) return null;
    const snap = await getDoc(userDoc());
    return snap.exists() ? (snap.data().activeFast || null) : null;
  },
  async setActiveFast(obj) {
    requireUser();
    await setDoc(userDoc(), { activeFast: obj }, { merge: true });
  },
  async clearActiveFast() {
    requireUser();
    await setDoc(userDoc(), { activeFast: null }, { merge: true });
  },
  async saveFastingSession(session) {
    requireUser();
    return addDoc(collection(db, "users", uid(), "fastingHistory"), {
      ...session,
      createdAt: serverTimestamp()
    });
  },
  async getFastingHistory() {
    if (!uid()) return [];
    const q = query(collection(db, "users", uid(), "fastingHistory"), orderBy("createdAt", "desc"));
    const snap = await getDocs(q);
    return snap.docs.map((d) => ({ id: d.id, ...d.data() }));
  },
  async deleteFastingSession(id) {
    requireUser();
    await deleteDoc(doc(db, "users", uid(), "fastingHistory", id));
  }
};

// ===========================================================================
//  حقن واجهة المصادقة في القائمة (nav) على كل الصفحات
// ===========================================================================
function renderNavAuthUI(user) {
  whenReady(() => { actuallyRenderNav(user); renderProfileAvatar(user); });
}

// تحديث صورة الملف الشخصي في profile.html (إن وُجدت)
function renderProfileAvatar(user) {
  const av = document.getElementById("pf-avatar");
  if (!av || !user) return;
  const name = user.displayName || (user.email ? user.email.split("@")[0] : "حسابي");
  if (user.photoURL) {
    av.innerHTML = "";
    const img = document.createElement("img");
    img.src = user.photoURL;
    img.alt = name;
    img.referrerPolicy = "no-referrer";
    av.appendChild(img);
  } else {
    av.textContent = (name.trim()[0] || "؟").toUpperCase();
  }
}

function actuallyRenderNav(user) {
  const desktop = document.querySelector("header nav .d-none.d-lg-flex");
  const outer   = desktop ? desktop.parentElement : null; // صفّ القائمة الكامل (.d-flex)
  const mobile  = document.getElementById("mobileNav");

  // إزالة أي خانة سابقة لتفادي التكرار
  document.querySelectorAll(".fit-auth-slot").forEach((el) => el.remove());

  injectFastingLink(desktop, mobile);

  // خانة المصادقة (سطح المكتب): مثبّتة في أقصى اليسار من شريط القائمة
  if (outer) {
    const slot = buildSlot(user, false);
    slot.classList.add("d-none", "d-lg-flex");   // تظهر على الشاشات الكبيرة فقط
    slot.style.marginInlineStart = "auto";        // RTL: تدفعها لأقصى اليسار
    outer.appendChild(slot);
  } else if (desktop) {
    desktop.appendChild(buildSlot(user, false));
  }
  if (mobile) mobile.appendChild(buildSlot(user, true));
}

// يضيف رابط "الصيام" إلى القائمة (مرة واحدة) بعد رابط حاسبة السعرات
function injectFastingLink(desktop, mobile) {
  const active = (location.pathname.split("/").pop() || "") === "fasting.html";
  if (desktop && !desktop.querySelector('a[href="fasting.html"]')) {
    const a = document.createElement("a");
    a.href = "fasting.html"; a.className = "nav-link-item"; a.textContent = "الصيام";
    a.style.cssText = `color:${active ? "#f59e0b" : "#ccc"};text-decoration:none;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;`;
    const after = desktop.querySelector('a[href="calculator.html"]');
    if (after) after.insertAdjacentElement("afterend", a); else desktop.appendChild(a);
  }
  if (mobile && !mobile.querySelector('a[href="fasting.html"]')) {
    const a = document.createElement("a");
    a.href = "fasting.html"; a.textContent = "الصيام";
    a.style.cssText = `display:block;color:${active ? "#f59e0b" : "#ccc"};text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;`;
    const after = mobile.querySelector('a[href="calculator.html"]');
    if (after) after.insertAdjacentElement("afterend", a); else mobile.appendChild(a);
  }
}

function buildSlot(user, isMobile) {
  const slot = document.createElement("div");
  slot.className = "fit-auth-slot";
  slot.style.cssText = isMobile
    ? "border-top:1px solid #333;margin-top:8px;padding-top:8px;"
    : "display:flex;align-items:center;";

  if (!user) {
    // --- خارج الحساب: زر تسجيل الدخول ---
    const a = document.createElement("a");
    a.href = "auth.html";
    a.textContent = "تسجيل الدخول";
    a.style.cssText = isMobile
      ? "display:block;color:#f59e0b;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;font-weight:700;"
      : "background:#f59e0b;color:#111;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:700;font-size:0.95rem;padding:8px 18px;border-radius:8px;white-space:nowrap;transition:background 0.3s;";
    if (!isMobile) {
      a.onmouseenter = () => (a.style.background = "#d97706");
      a.onmouseleave = () => (a.style.background = "#f59e0b");
    }
    slot.appendChild(a);
    return slot;
  }

  // --- داخل الحساب: صورة + اسم + قائمة منسدلة ---
  const name  = user.displayName || (user.email ? user.email.split("@")[0] : "حسابي");
  const photo = user.photoURL;

  const avatar = document.createElement("span");
  avatar.style.cssText =
    "width:34px;height:34px;border-radius:50%;flex:0 0 auto;display:inline-flex;align-items:center;justify-content:center;background:#f59e0b;color:#111;font-weight:800;font-family:'Tajawal',sans-serif;overflow:hidden;";
  if (photo) {
    const img = document.createElement("img");
    img.src = photo;
    img.alt = name;
    img.referrerPolicy = "no-referrer";
    img.style.cssText = "width:100%;height:100%;object-fit:cover;";
    avatar.appendChild(img);
  } else {
    avatar.textContent = (name.trim()[0] || "؟").toUpperCase();
  }

  if (isMobile) {
    // الموبايل: روابط مباشرة بدون منسدلة
    const head = document.createElement("div");
    head.style.cssText = "display:flex;align-items:center;gap:10px;padding:10px 0;";
    const label = document.createElement("span");
    label.textContent = name;
    label.style.cssText = "color:#fff;font-family:'Tajawal',sans-serif;font-weight:700;";
    head.appendChild(avatar);
    head.appendChild(label);
    slot.appendChild(head);

    slot.appendChild(mobileLink("ملفي الشخصي", () => (location.href = "profile.html")));
    slot.appendChild(mobileLink("تسجيل الخروج", () => window.FITAuth.logout(), "#e94560"));
    return slot;
  }

  // الديسكتوب: زر يفتح قائمة منسدلة
  slot.style.position = "relative";

  const btn = document.createElement("button");
  btn.type = "button";
  btn.style.cssText =
    "display:flex;align-items:center;gap:8px;background:none;border:1px solid #f59e0b55;border-radius:30px;padding:4px 12px 4px 6px;cursor:pointer;color:#fff;font-family:'Tajawal',sans-serif;";
  const btnLabel = document.createElement("span");
  btnLabel.textContent = name;
  btnLabel.style.cssText = "font-weight:600;font-size:0.95rem;max-width:120px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;";
  const caret = document.createElement("i");
  caret.className = "fas fa-chevron-down";
  caret.style.cssText = "font-size:0.7rem;color:#f59e0b;";
  btn.appendChild(avatar);
  btn.appendChild(btnLabel);
  btn.appendChild(caret);

  const menu = document.createElement("div");
  menu.style.cssText =
    "position:absolute;top:calc(100% + 8px);left:0;min-width:180px;background:#1a1a1a;border:1px solid #f59e0b33;border-radius:10px;padding:6px;display:none;flex-direction:column;z-index:10000;box-shadow:0 10px 30px rgba(0,0,0,.5);";
  menu.appendChild(menuItem("ملفي الشخصي", "fa-user", () => (location.href = "profile.html")));
  menu.appendChild(menuItem("تسجيل الخروج", "fa-right-from-bracket", () => window.FITAuth.logout(), "#e94560"));

  btn.addEventListener("click", (e) => {
    e.stopPropagation();
    menu.style.display = menu.style.display === "flex" ? "none" : "flex";
  });
  document.addEventListener("click", () => { menu.style.display = "none"; });

  slot.appendChild(btn);
  slot.appendChild(menu);
  return slot;
}

function mobileLink(text, onClick, color = "#ccc") {
  const a = document.createElement("a");
  a.href = "#";
  a.textContent = text;
  a.style.cssText = `display:block;color:${color};text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;`;
  a.addEventListener("click", (e) => { e.preventDefault(); onClick(); });
  return a;
}

function menuItem(text, icon, onClick, color = "#fff") {
  const a = document.createElement("a");
  a.href = "#";
  a.style.cssText = `display:flex;align-items:center;gap:10px;color:${color};text-decoration:none;padding:10px 12px;border-radius:8px;font-family:'Tajawal',sans-serif;font-size:0.95rem;`;
  const i = document.createElement("i");
  i.className = "fas " + icon;
  i.style.cssText = "font-size:0.85rem;opacity:.8;";
  const span = document.createElement("span");
  span.textContent = text;
  a.appendChild(i);
  a.appendChild(span);
  a.addEventListener("mouseenter", () => (a.style.background = "#ffffff10"));
  a.addEventListener("mouseleave", () => (a.style.background = "transparent"));
  a.addEventListener("click", (e) => { e.preventDefault(); onClick(); });
  return a;
}
