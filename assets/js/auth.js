// ============================================================================
//  MAKE ME FIT — وحدة المصادقة والبيانات (API client)
// ----------------------------------------------------------------------------
//  ملف ES module واحد يتحدث مع واجهة ASP.NET Core على نفس الأصل عبر fetch.
//  لا يستورد Firebase. يُحمّل في كل صفحة عبر:
//    <script type="module" src="assets/js/auth.js"></script>
//
//  يكشف جسرَين عالميَّين للكود القديم (jQuery + inline onclick):
//    window.FITAuth  — تسجيل الدخول/الخروج وحالة المستخدم
//    window.FITData  — قراءة/كتابة بيانات المستخدم عبر REST API
// ============================================================================

// ---------------------------------------------------------------------------
//  حالة الوحدة
// ---------------------------------------------------------------------------
let currentUser = null;     // كائن المستخدم الحالي أو null
let authResolved = false;   // هل حُسمت حالة المصادقة الأولى؟
const userListeners = [];   // مستمعو تغيّر المستخدم

// ---------------------------------------------------------------------------
//  مساعد fetch موحّد — يرسل الكوكي دائماً ويرفع Error.code من ProblemDetails
// ---------------------------------------------------------------------------
async function api(path, { method = "GET", body, raw } = {}) {
  const opts = { method, credentials: "include", headers: {} };
  if (raw !== undefined) {
    opts.body = raw;                       // FormData أو ما شابه — لا نضبط Content-Type
  } else if (body !== undefined) {
    opts.headers["Content-Type"] = "application/json";
    opts.body = JSON.stringify(body);
  }

  let res;
  try {
    res = await fetch(path, opts);
  } catch (e) {
    const err = new Error("network");
    err.code = "network";
    throw err;
  }

  let data = null;
  const text = await res.text();
  if (text) { try { data = JSON.parse(text); } catch (_) { data = null; } }

  if (!res.ok) {
    const code = (data && (data.code || (data.extensions && data.extensions.code))) || String(res.status);
    const err = new Error(code);
    err.code = code;
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

// ---------------------------------------------------------------------------
//  تحويل UserDto القادم من الخادم إلى الشكل الذي تتوقعه الصفحات
//  (يكشف uid و id معاً — uid() في FITData يقرأ currentUser.uid)
// ---------------------------------------------------------------------------
function mapUser(dto) {
  if (!dto) return null;
  return {
    uid: dto.id,
    id: dto.id,
    displayName: dto.displayName || "",
    email: dto.email || "",
    photoURL: dto.photoUrl || null,
    emailVerified: !!dto.emailConfirmed
  };
}

function whenReady(fn) {
  if (document.readyState !== "loading") fn();
  else document.addEventListener("DOMContentLoaded", fn);
}

// تُستدعى عند حسم حالة المصادقة
function notify(user) {
  currentUser = user || null;
  authResolved = true;
  renderNavAuthUI(currentUser);
  userListeners.forEach((cb) => { try { cb(currentUser); } catch (e) { console.error(e); } });
}

// ---------------------------------------------------------------------------
//  تهيئة: اقرأ حالة الجلسة الحالية من الخادم (يحلّ محل onAuthStateChanged)
// ---------------------------------------------------------------------------
(async function bootstrap() {
  try {
    const dto = await api("/api/auth/me");
    notify(mapUser(dto));
  } catch (e) {
    notify(null);   // 401 أو خطأ شبكة => لا يوجد مستخدم
  }
})();

// ===========================================================================
//  واجهة المصادقة  window.FITAuth
// ===========================================================================
async function signInGoogle() {
  location.href = "/api/auth/google";
}

async function registerEmail({ name, email, password }) {
  await api("/api/auth/register", { method: "POST", body: { name, email, password } });
  // بعد التسجيل سجّل الدخول مباشرةً لتثبيت الجلسة (الكوكي)
  return loginEmail({ email, password });
}

async function loginEmail({ email, password }) {
  const dto = await api("/api/auth/login", { method: "POST", body: { email, password } });
  notify(mapUser(dto));
  return currentUser;
}

async function resetPassword(email) {
  return api("/api/auth/forgot-password", { method: "POST", body: { email } });
}

async function resendVerification() {
  return api("/api/auth/resend-verification", { method: "POST" });
}

async function logout() {
  try { await api("/api/auth/logout", { method: "POST" }); }
  catch (e) { console.warn("[FIT] logout:", e); }
  notify(null);
  location.href = "index.html";
}

// يحوّل رموز أخطاء الـ API إلى رسائل عربية (نفس صياغة النسخة السابقة)
function errMessage(code) {
  const map = {
    "email-already-in-use": "هذا البريد مسجّل بالفعل. جرّب تسجيل الدخول.",
    "invalid-credential":   "البريد أو كلمة المرور غير صحيحة.",
    "invalid-token":        "رابط إعادة التعيين غير صالح أو منتهي الصلاحية.",
    "bad-avatar":           "يجب اختيار ملف صورة صالح أقل من ٢ ميجابايت.",
    "bad-weight":           "الوزن غير صحيح (١٠–٥٠٠ كجم).",
    "network":              "تعذّر الاتصال بالشبكة. تحقّق من اتصالك."
  };
  return map[code] || "حدث خطأ غير متوقع. حاول مرة أخرى.";
}

window.FITAuth = {
  get user() { return currentUser; },
  get ready() { return true; },
  signInGoogle,
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

  // رفع صورة المستخدم عبر multipart إلى /api/profile/avatar ثم تحديث الواجهة
  async uploadAvatar(file) {
    if (!currentUser) throw new Error("يجب تسجيل الدخول أولاً");
    if (!file || !file.type || !file.type.startsWith("image/")) {
      const err = new Error("bad-avatar"); err.code = "bad-avatar"; throw err;
    }
    if (file.size > 2 * 1024 * 1024) {
      const err = new Error("bad-avatar"); err.code = "bad-avatar"; throw err;
    }
    const fd = new FormData();
    fd.append("file", file);
    const data = await api("/api/profile/avatar", { method: "POST", raw: fd });
    const url = data && data.photoURL;
    currentUser = { ...currentUser, photoURL: url || null };
    renderNavAuthUI(currentUser);
    return url;
  },

  // إعادة رسم الواجهة (القائمة + صورة الملف) من حالة المستخدم الحالية
  refreshUI() { renderNavAuthUI(currentUser); }
};

// ===========================================================================
//  طبقة البيانات  window.FITData  (REST API)
// ===========================================================================
function uid() { return currentUser ? currentUser.uid : null; }

window.FITData = {
  // --- الملف الشخصي ---
  async getProfile() {
    if (!uid()) return null;
    try { return await api("/api/profile"); }
    catch (e) { return null; }
  },
  async saveProfile(partial) {
    return api("/api/profile", { method: "PUT", body: partial || {} });
  },

  // --- التمارين المفضلة ---
  async getFavorites() {
    if (!uid()) return [];
    try { return await api("/api/favorites"); }
    catch (e) { return []; }
  },
  // يضيف أو يزيل حسب وجود المعرّف؛ يعيد true إذا أصبح مفضّلاً
  async toggleFavorite(fav) {
    const res = await api("/api/favorites", { method: "POST", body: fav });
    return !!(res && res.favorited);
  },
  async removeFavorite(id) {
    return api(`/api/favorites/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- سجل حاسبة السعرات ---
  async saveCalcResult(result) {
    return api("/api/calc-history", { method: "POST", body: result });
  },
  async getCalcHistory() {
    if (!uid()) return [];
    try { return await api("/api/calc-history"); }
    catch (e) { return []; }
  },
  async deleteCalcResult(id) {
    return api(`/api/calc-history/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- خطط التغذية المحفوظة ---
  async getNutritionPlans() {
    if (!uid()) return [];
    try { return await api("/api/nutrition-plans"); }
    catch (e) { return []; }
  },
  async saveNutritionPlan(plan) {
    const res = await api("/api/nutrition-plans", { method: "POST", body: plan });
    return !!(res && res.saved);
  },
  async removeNutritionPlan(id) {
    return api(`/api/nutrition-plans/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- سجل الوزن والقياسات (نفس رسالة التحقق العربية قبل الإرسال) ---
  async addWeightEntry(entry) {
    const { weight, waist, chest, arms, date } = entry || {};
    const w = Number(weight);
    if (!w || w < 10 || w > 500) {
      const err = new Error("الوزن غير صحيح (١٠–٥٠٠ كجم)");
      err.code = "bad-weight";
      throw err;
    }
    return api("/api/weight-log", {
      method: "POST",
      body: {
        weight: w,
        waist: waist ? Number(waist) : null,
        chest: chest ? Number(chest) : null,
        arms:  arms  ? Number(arms)  : null,
        date:  date || null
      }
    });
  },
  async getWeightLog() {
    if (!uid()) return [];
    try { return await api("/api/weight-log"); }
    catch (e) { return []; }
  },
  async deleteWeightEntry(id) {
    return api(`/api/weight-log/${encodeURIComponent(id)}`, { method: "DELETE" });
  },

  // --- متابعة إنجاز التمارين + السلسلة (streak) ---
  async getCompletedDates() {
    if (!uid()) return [];
    try { return await api("/api/workouts/completed"); }
    catch (e) { return []; }
  },
  async markWorkoutToday() {
    return api("/api/workouts/complete", { method: "POST" });
  },
  async unmarkWorkoutToday() {
    return api("/api/workouts/complete", { method: "DELETE" });
  },

  // --- خطتي المخصصة ---
  async getPlan() {
    if (!uid()) return [];
    try { return await api("/api/plan"); }
    catch (e) { return []; }
  },
  async addToPlan(exercise) {
    const res = await api("/api/plan", {
      method: "POST",
      body: {
        id: exercise.id,
        nameAr: exercise.nameAr || "",
        nameEn: exercise.nameEn || "",
        tab: exercise.tab || "",
        sets: exercise.sets ?? null,
        reps: exercise.reps ?? null
      }
    });
    return !!(res && res.added);
  },
  async updatePlan(list) {
    return api("/api/plan", { method: "PUT", body: list || [] });
  },
  async removeFromPlan(id) {
    return api(`/api/plan/${encodeURIComponent(id)}`, { method: "DELETE" });
  },
  async clearPlan() {
    return api("/api/plan", { method: "DELETE" });
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
