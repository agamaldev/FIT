# إعداد وتشغيل الخادم (PostgreSQL + ASP.NET Core) — MAKE ME FIT

تمت ترقية الموقع من **Firebase** إلى خادم ذاتي الاستضافة: **ASP.NET Core Web API** + قاعدة بيانات **PostgreSQL**، كلاهما داخل **Docker**. الـAPI نفسه **يخدم صفحات الموقع الثابتة** على نفس المنفذ (أصل واحد / single origin)، فلا حاجة لأي خادم ملفات منفصل.

> النظام يشمل: حسابات (دخول عبر **Google** أو **البريد/كلمة المرور**) + **مساحة شخصية** (ملف شخصي + صورة قابلة للرفع + تمارين مفضلة + سجل حاسبة السعرات + خطط تغذية محفوظة + متابع وزن وقياسات + متابعة إنجاز التمارين وسلسلة الأيام + منشئ خطة مخصصة + الصيام المتقطع). دخول **Microsoft** لم يعد مدعوماً.

---

## المتطلبات المسبقة

- **Docker Desktop** (يتضمن `docker compose`) مثبّت ويعمل.
- **.NET 10 SDK** — لازم فقط لتشغيل الاختبارات محلياً (الخطوة "تشغيل الاختبارات"). التشغيل العادي عبر Docker لا يحتاجه.

---

## التشغيل السريع

من داخل مجلد المشروع `D:\Work\Templates\FIT`:

```bash
docker compose up --build
```

يبني هذا الأمر ثلاث خدمات ويشغّلها:

| الخدمة | الصورة | الدور |
|--------|--------|-------|
| `db`   | `postgres:16`     | قاعدة بيانات PostgreSQL |
| `api`  | (يُبنى من `server/FitApi/Dockerfile`) | الـAPI + خدمة صفحات الموقع |
| `mail` | `axllent/mailpit` | خادم بريد تجريبي يلتقط الرسائل (تأكيد البريد / إعادة تعيين كلمة المرور) |

### العناوين بعد التشغيل

| الغرض | العنوان |
|------|---------|
| الموقع (الصفحة الرئيسية) | <http://localhost:8080> |
| فحص صحة الـAPI | <http://localhost:8080/api/health> → `{"status":"ok"}` |
| صندوق بريد Mailpit (لعرض رسائل التأكيد) | <http://localhost:8025> |

> الهجرات (migrations) تُطبَّق تلقائياً عند إقلاع الـAPI عبر `AppDbContext.Database.Migrate()`، فلا تحتاج خطوة يدوية لإنشاء الجداول. البيانات تبدأ فارغة (greenfield).

للإيقاف: `Ctrl+C` ثم `docker compose down`. لمسح قاعدة البيانات أيضاً: `docker compose down -v`.

---

## بديل للتطوير: قاعدة SQLite محلية (بدون Docker)

للتطوير السريع دون تشغيل Docker/PostgreSQL، يدعم التطبيق قاعدة **SQLite** (ملف واحد) **جنباً إلى جنب** مع PostgreSQL. يُختار المزوّد عبر الإعداد `Database:Provider`:

- بيئة **التطوير** (Development) → الافتراضي **SQLite** (`server/FitApi/appsettings.Development.json`، وسلسلة الاتصال `Data Source=fit.db`).
- **Docker/الإنتاج** → الافتراضي **PostgreSQL** (`appsettings.json` + `docker-compose.yml`).

التشغيل محلياً (يتطلب **.NET 10 SDK** فقط — لا Docker ولا بريد):

```bash
cd "D:/Work/Templates/FIT/server/FitApi"
dotnet run
```

- يُنشَأ ملف `fit.db` تلقائياً عبر `Database.EnsureCreated()` (يبني المخطط من النماذج مباشرةً — لا هجرات مع SQLite؛ أعمدة `jsonb` تُخزَّن كنص `TEXT`). الملف مُستثنى من Git.
- لا يحتاج خادم بريد: إرسال رسالة التأكيد "أفضل-جهد"، فلا يفشل التسجيل إن غاب SMTP.
- لإجبار مزوّد صراحةً (يتجاوز الافتراضي): `Database__Provider=Sqlite` أو `Database__Provider=Postgres` كمتغيّر بيئة.

> ملاحظة: الاختبارات (`dotnet test`) تظل تعمل على PostgreSQL عبر Testcontainers بصرف النظر عن هذا الإعداد.

---

## استخدم الموقع عبر http (وليس بالنقر المزدوج)

⚠️ افتح الموقع دائماً عبر **<http://localhost:8080>**. لم يعد فتح ملفات `.html` بالنقر المزدوج (`file://`) أسلوباً مدعوماً — والأهم أن **سبب الأعطال القديم اختفى بنيوياً**: لأن الـAPI نفسه هو من يخدم الصفحات الآن، تعمل وحدات الـJS (ES modules) ونداءات الـAPI وملفات الكوكيز كلها على نفس الأصل دون مشاكل CORS أو قيود `file://`.

---

## الإعداد: Google ودخول البريد (SMTP)

### دخول Google

أنشئ **OAuth 2.0 Client ID** من [console.cloud.google.com](https://console.cloud.google.com) → **APIs & Services** → **Credentials** → **Create credentials** → **OAuth client ID** (نوع **Web application**)، وأضِف في **Authorized redirect URIs**:

```
http://localhost:8080/signin-google
```

ثم زوّد الـAPI بالقيمتين عبر متغيرات البيئة في `docker-compose.yml` (ضمن قسم `environment` للخدمة `api`)؛ صيغة المتغير في ASP.NET Core تستبدل النقطتين `:` بشرطتين سفليتين `__`:

```yaml
services:
  api:
    environment:
      - Authentication__Google__ClientId=ضع-الـClientId-هنا
      - Authentication__Google__ClientSecret=ضع-الـClientSecret-هنا
```

بديلاً عن ذلك يمكن وضع نفس القيم في `server/FitApi/appsettings.json` تحت المفتاح `Authentication:Google` — لكن المفضّل أثناء التطوير هو متغيرات البيئة كي لا تُحفظ الأسرار في الكود.

### البريد (SMTP) — تأكيد البريد وإعادة تعيين كلمة المرور

أثناء التطوير، الإعدادات الافتراضية موجّهة لخدمة **Mailpit** داخل الشبكة (`Smtp:Host=mail`, `Smtp:Port=1025`)، فترى كل رسائل التأكيد/الاستعادة على <http://localhost:8025> دون إرسال حقيقي. للإنتاج، عدّل القيم عبر متغيرات البيئة للخدمة `api`:

```yaml
services:
  api:
    environment:
      - Smtp__Host=smtp.yourprovider.com
      - Smtp__Port=587
      - Smtp__From=no-reply@yoursite.com
```

أو ضعها في `appsettings.json` تحت المفتاح `Smtp`.

---

## متغيرات البيئة المطلوبة

| المفتاح (داخل التطبيق) | صيغة متغير البيئة | القيمة الافتراضية (تطوير) | الغرض |
|---|---|---|---|
| `Database:Provider` | `Database__Provider` | `Sqlite` (تطوير) / `Postgres` (Docker) | اختيار مزوّد قاعدة البيانات: `Sqlite` أو `Postgres` |
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | SQLite: `Data Source=fit.db` — Docker: سلسلة اتصال خدمة `db` | سلسلة اتصال قاعدة البيانات (حسب المزوّد) |
| `Authentication:Google:ClientId` | `Authentication__Google__ClientId` | — (يُضبط منك) | دخول Google |
| `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` | — (يُضبط منك) | دخول Google |
| `Smtp:Host` | `Smtp__Host` | `mail` | خادم البريد |
| `Smtp:Port` | `Smtp__Port` | `1025` | منفذ البريد |
| `Smtp:From` | `Smtp__From` | عنوان مرسِل افتراضي | مرسِل الرسائل |
| `Storage:AvatarRoot` | `Storage__AvatarRoot` | `/app/uploads/avatars` | مجلد حفظ صور البروفايل |
| `Frontend:WebRoot` | `Frontend__WebRoot` | جذر الموقع داخل الحاوية | المجلد الذي يخدم منه الـAPI صفحات الموقع |

> ملاحظة: `Frontend:WebRoot` تُضبط داخل صورة الـAPI لتشير إلى مجلد ملفات الموقع؛ لا تحتاج تغييرها للتشغيل العادي. صور البروفايل تُخدَّم على المسار `/uploads/avatars/...`.

---

## تشغيل الهجرات (migrations)

تُطبَّق الهجرات **تلقائياً** عند كل إقلاع للـAPI، فلا حاجة لخطوة يدوية في التشغيل المعتاد. عند تعديل النماذج (Models) وإنشاء هجرة جديدة، شغّل من داخل `D:\Work\Templates\FIT\server`:

```bash
cd "D:/Work/Templates/FIT/server"
dotnet ef migrations add <اسم-الهجرة> --project FitApi
```

ثم أعد بناء الحاويات (`docker compose up --build`) لتُطبَّق الهجرة الجديدة آلياً عند الإقلاع. (تثبيت الأداة عند الحاجة: `dotnet tool install --global dotnet-ef`.)

---

## تشغيل الاختبارات

الاختبارات تستخدم **Testcontainers** فتشغّل حاوية PostgreSQL خاصة بها تلقائياً — لذا يجب أن يكون **Docker Desktop يعمل**. من داخل `D:\Work\Templates\FIT\server`:

```bash
cd "D:/Work/Templates/FIT/server"
dotnet test
```

تنتهي كل الاختبارات بنجاح (Passed!) دون أي إعداد مسبق لقاعدة بيانات — الـCustomWebApplicationFactory يهيّئ قاعدة معزولة لكل تشغيل، والهجرات تُطبَّق آلياً عند إقلاع التطبيق داخل الاختبار.

---

## ملاحظات

- لم تعد هناك أي تبعية على Firebase: حُذفت ملفات `assets/js/firebase-config.js` و`firestore.rules` و`storage.rules` و`FIREBASE-SETUP.md`. كل المنطق صار في `assets/js/auth.js` (الذي يستدعي الـAPI عبر `fetch` مع `credentials:"include"`).
- عزل بيانات المستخدمين يتم على مستوى الـAPI: كل استعلام مقيَّد بمعرّف المستخدم المستخرج من ملف الكوكيز (الجلسة)، ولا يُقرأ معرّف المستخدم أبداً من جسم الطلب أو مساره.
- القائمة (nav) ما زالت مكررة يدوياً في كل صفحة (تصميم القالب الأصلي)؛ واجهة الدخول تُحقن تلقائياً عبر الـJS. لإضافة صفحة جديدة أضِف لها فقط:
  `<script type="module" src="assets/js/auth.js"></script>`
