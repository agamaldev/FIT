# MAKE ME FIT Website Rebuild - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace all content on the MAKE ME FIT website with data from 4 fitness PDF sources, restructure pages (Home, Warm-ups, Workouts, Cardio, Nutrition, Calculator, Contact), and add YouTube video modal popups.

**Architecture:** Static HTML site with Bootstrap 5, inline styles per page, vanilla JS for tab switching and video modals. Each page is self-contained with its own header/footer. RTL Arabic throughout. Content manually transcribed from PDFs into HTML.

**Tech Stack:** HTML5, CSS3, Bootstrap 5, jQuery 3.7.1, AOS.js, Font Awesome, Tajawal + Zen Dots fonts

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `index.html` | Modify | Home page - hero, 5 feature cards, program cards, CTA, footer |
| `warmup.html` | Create | Warm-up guides - tips, cardio warm-up, upper/lower body warm-up |
| `workouts.html` | Create | 7-tab workout programs with exercise cards and video buttons |
| `cardio.html` | Create | Cardio machines + ab exercises |
| `nutrition.html` | Modify | 6-tab nutrition plans with meal cards and macros |
| `calculator.html` | Modify | Update nav links only (keep calculator logic) |
| `contact.html` | Modify | Update nav links only (keep contact form) |
| `muscles.html` | Delete | No longer needed |
| `equipment.html` | Delete | No longer needed |
| `exercises.html` | Delete | Replaced by workouts.html |

## Data Extraction Reference

Video URLs are embedded in the PDFs as hyperlinks. Each exercise page in the PDF contains YouTube links per exercise. The extraction script `python -c "import fitz; ..."` (shown in spec research) maps page numbers to unique YouTube URLs.

**Key PDF page ranges:**
- 1.pdf pages 10-27: 4/5/6-day splits with YouTube links
- 2.pdf pages 5-55: Fitness Factory exercises with YouTube links
- جدول تمارين.pdf pages 4-7: warm-ups, cardio, abs
- جدول تمارين.pdf pages 8-69: all workout programs with YouTube links
- جدول تمارين.pdf pages 75-112: nutrition plans (no video links)
- تغذية.pdf pages 1-43: Ramadan nutrition plans (no links)

---

## Task 1: Initialize Git repo and set up shared components

**Files:**
- Modify: `D:/Work/Templates/FIT/index.html` (extract shared patterns)

This task establishes the git repo and documents the shared HTML patterns (nav, footer, video modal) that all pages will use.

- [ ] **Step 1: Initialize git repository**

```bash
cd "D:/Work/Templates/FIT"
git init
git add -A
git commit -m "Initial commit: existing MAKE ME FIT website"
```

- [ ] **Step 2: Create shared nav HTML reference**

All pages will use this exact nav bar. Save this as a reference comment at the top of each new page. The nav links are updated to the new page structure:

```html
<!-- Shared Nav - copy to all pages -->
<header>
    <nav style="background:#111;padding:15px 0;position:fixed;top:0;left:0;right:0;z-index:9999;border-bottom:1px solid #ff572233;direction:rtl;">
        <div class="container-fluid px-4">
            <div class="d-flex align-items-center" style="direction:rtl;gap:40px;">
                <a href="index.html" style="text-decoration:none;font-family:'Zen Dots',cursive;font-size:1.6rem;white-space:nowrap;">
                    <span style="color:#ff5722;">MAKE ME</span> <span style="color:#fff;">FIT</span>
                </a>
                <div class="d-none d-lg-flex gap-3 align-items-center">
                    <a href="index.html" class="nav-link-item" style="color:#ccc;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;">الرئيسية</a>
                    <a href="warmup.html" class="nav-link-item" style="color:#ccc;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;">الإحماء</a>
                    <a href="workouts.html" class="nav-link-item" style="color:#ccc;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;">التمارين</a>
                    <a href="cardio.html" class="nav-link-item" style="color:#ccc;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;">الكارديو</a>
                    <a href="nutrition.html" class="nav-link-item" style="color:#ccc;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;">التغذية</a>
                    <a href="calculator.html" class="nav-link-item" style="color:#ccc;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;">حاسبة السعرات</a>
                    <a href="contact.html" class="nav-link-item" style="color:#ccc;text-decoration:none;font-family:'Tajawal',sans-serif;font-weight:500;font-size:1rem;padding:8px 12px;transition:color 0.3s;">تواصل معنا</a>
                </div>
                <button class="d-lg-none" style="background:none;border:1px solid #444;color:#fff;padding:8px 14px;border-radius:6px;cursor:pointer;font-size:1.2rem;" onclick="document.getElementById('mobileNav').classList.toggle('show')">
                    <i class="fas fa-bars"></i>
                </button>
            </div>
            <div id="mobileNav" class="d-lg-none" style="display:none;padding-top:15px;border-top:1px solid #333;margin-top:15px;">
                <a href="index.html" style="display:block;color:#ccc;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;">الرئيسية</a>
                <a href="warmup.html" style="display:block;color:#ccc;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;">الإحماء</a>
                <a href="workouts.html" style="display:block;color:#ccc;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;">التمارين</a>
                <a href="cardio.html" style="display:block;color:#ccc;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;">الكارديو</a>
                <a href="nutrition.html" style="display:block;color:#ccc;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;">التغذية</a>
                <a href="calculator.html" style="display:block;color:#ccc;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;">حاسبة السعرات</a>
                <a href="contact.html" style="display:block;color:#ccc;text-decoration:none;padding:10px 0;font-family:'Tajawal',sans-serif;">تواصل معنا</a>
            </div>
        </div>
    </nav>
    <div style="height:70px;"></div>
</header>
<style>
    .nav-link-item:hover { color: #ff5722 !important; }
    #mobileNav.show { display: block !important; }
</style>
<script>
    document.getElementById('mobileNav')?.addEventListener('click', function(e) {
        if (e.target.tagName === 'A') this.classList.remove('show');
    });
</script>
```

**Active page:** On each page, change the relevant nav link from `color:#ccc` to `color:#ff5722`.

- [ ] **Step 3: Create shared video modal HTML/CSS/JS reference**

All pages with video buttons will include this at the bottom before the closing `</body>` tag:

```html
<!-- Video Modal -->
<div id="videoModal" style="display:none;position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.9);z-index:99999;align-items:center;justify-content:center;">
    <div style="position:relative;width:90%;max-width:800px;">
        <button onclick="closeVideoModal()" style="position:absolute;top:-40px;right:0;background:none;border:none;color:#fff;font-size:2rem;cursor:pointer;z-index:1;">&times;</button>
        <div style="position:relative;padding-bottom:56.25%;height:0;overflow:hidden;border-radius:8px;">
            <iframe id="videoFrame" style="position:absolute;top:0;left:0;width:100%;height:100%;border:none;" allowfullscreen></iframe>
        </div>
    </div>
</div>
<script>
function openVideoModal(url) {
    // Convert youtu.be and youtube shorts to embed format
    let videoId = '';
    if (url.includes('youtu.be/')) {
        videoId = url.split('youtu.be/')[1].split('?')[0];
    } else if (url.includes('youtube.com/shorts/')) {
        videoId = url.split('shorts/')[1].split('?')[0];
    } else if (url.includes('youtube.com/watch')) {
        const params = new URL(url).searchParams;
        videoId = params.get('v');
    }
    if (videoId) {
        document.getElementById('videoFrame').src = 'https://www.youtube.com/embed/' + videoId + '?autoplay=1';
        document.getElementById('videoModal').style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }
}
function closeVideoModal() {
    document.getElementById('videoFrame').src = '';
    document.getElementById('videoModal').style.display = 'none';
    document.body.style.overflow = '';
}
document.getElementById('videoModal').addEventListener('click', function(e) {
    if (e.target === this) closeVideoModal();
});
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') closeVideoModal();
});
</script>
```

**Video button pattern** used in exercise cards:

```html
<button onclick="openVideoModal('https://youtu.be/VIDEO_ID')" style="background:#ff5722;border:none;color:#fff;padding:8px 20px;border-radius:6px;cursor:pointer;font-family:'Tajawal',sans-serif;font-size:0.9rem;transition:all 0.3s;">
    <i class="fas fa-play" style="margin-left:5px;"></i> شاهد الفيديو
</button>
```

- [ ] **Step 4: Create shared footer HTML reference**

```html
<!-- Footer - copy to all pages -->
<footer>
    <div class="footer_sec bg_black">
        <div class="container">
            <div class="footer_area">
                <div class="row">
                    <div class="col-12">
                        <div class="footer_upper_big_text sec_padding">
                            <div class="reveal custom_lightSpeedInLeft">
                                <a href="contact.html" class="contact_hover_btn">
                                    <svg viewBox="0 0 1300 128">
                                        <symbol id="s-text">
                                            <text text-anchor="middle" x="50%" y="50%" dy=".35em">MAKE ME FIT</text>
                                        </symbol>
                                        <use class="text" xlink:href="#s-text"></use>
                                        <use class="text" xlink:href="#s-text"></use>
                                        <use class="text" xlink:href="#s-text"></use>
                                        <use class="text" xlink:href="#s-text"></use>
                                        <use class="text" xlink:href="#s-text"></use>
                                    </svg>
                                </a>
                            </div>
                        </div>
                    </div>
                    <div class="col-12">
                        <div class="footer_mid_area">
                            <div class="row align-items-start">
                                <div class="col-xl-4 col-lg-4 col-sm-6">
                                    <div class="footer_logo_area">
                                        <a href="index.html" class="footer_logo reveal custom_zoom_in d-block">
                                            <img src="assets/images/logos/makemefit_logo.svg" alt="MAKE ME FIT Logo">
                                        </a>
                                        <p class="line_height_30 fw_500 color_lightgray reveal custom_zoom_in" style="margin-top: 15px;">
                                            ميك مي فت - دليلك الشامل لعالم اللياقة البدنية. نقدم لك كل ما تحتاجه من تمارين وتغذية وجداول تدريبية لبناء جسمك المثالي.
                                        </p>
                                    </div>
                                </div>
                                <div class="col-xl-2 col-lg-2 col-sm-6">
                                    <div class="footer_quick_links">
                                        <h6 class="color_white reveal custom_lightSpeedInLeft mb-3">روابط سريعة</h6>
                                        <ul>
                                            <li class="reveal custom_zoom_in"><a href="index.html">الرئيسية</a></li>
                                            <li class="reveal custom_zoom_in"><a href="warmup.html">الإحماء</a></li>
                                            <li class="reveal custom_zoom_in"><a href="workouts.html">التمارين</a></li>
                                            <li class="reveal custom_zoom_in"><a href="cardio.html">الكارديو</a></li>
                                        </ul>
                                    </div>
                                </div>
                                <div class="col-xl-3 col-lg-3 col-sm-6">
                                    <div class="footer_quick_links">
                                        <h6 class="color_white reveal custom_lightSpeedInLeft mb-3">المزيد</h6>
                                        <ul>
                                            <li class="reveal custom_zoom_in"><a href="nutrition.html">التغذية</a></li>
                                            <li class="reveal custom_zoom_in"><a href="calculator.html">حاسبة السعرات</a></li>
                                            <li class="reveal custom_zoom_in"><a href="contact.html">تواصل معنا</a></li>
                                        </ul>
                                    </div>
                                </div>
                                <div class="col-xl-3 col-lg-3 col-sm-6">
                                    <div class="footer_address pb-30">
                                        <h6 class="color_white reveal custom_lightSpeedInLeft mb-3">تواصل معنا</h6>
                                        <p class="line_height_30 fw_500 color_lightgray reveal custom_zoom_in">
                                            <a href="mailto:info@makemefit.com" class="color_lightgray triners_icons">info@makemefit.com</a>
                                        </p>
                                        <p class="line_height_30 fw_500 color_lightgray reveal custom_zoom_in" style="margin-top: 10px;">
                                            موقع متخصص في اللياقة البدنية والتغذية الرياضية
                                        </p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-12">
                        <div class="footer_bottom_area">
                            <div class="row align-items-center">
                                <div class="col-sm-6">
                                    <p class="line_height_24 fw_500 color_lightgray reveal custom_zoom_in">
                                        جميع الحقوق محفوظة &copy; 2024 MAKE ME FIT - ميك مي فت
                                    </p>
                                </div>
                                <div class="col-sm-6">
                                    <ul class="footer_social_icon">
                                        <li class="reveal custom_zoom_in"><a href="#" class="triners_icons" target="_blank"><i class="fab fa-facebook-f"></i></a></li>
                                        <li class="reveal custom_zoom_in"><a href="#" class="triners_icons" target="_blank"><i class="fab fa-twitter"></i></a></li>
                                        <li class="reveal custom_zoom_in"><a href="#" class="triners_icons" target="_blank"><i class="fab fa-instagram"></i></a></li>
                                        <li class="reveal custom_zoom_in"><a href="#" class="triners_icons" target="_blank"><i class="fab fa-youtube"></i></a></li>
                                    </ul>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</footer>
```

- [ ] **Step 5: Create shared `<head>` reference**

All pages use this exact `<head>` block (only `<title>` changes per page):

```html
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MAKE ME FIT - PAGE_TITLE_HERE</title>
    <link rel="shortcut icon" href="assets/images/favicon/favicon.ico" type="image/x-icon">
    <link href="https://fonts.googleapis.com/css2?family=Zen+Dots&display=swap" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Tajawal:wght@300;400;500;700;800;900&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="assets/css/all.min.css">
    <link rel="stylesheet" href="assets/css/slick.css">
    <link rel="stylesheet" href="assets/css/animate_plugin.min.css">
    <link rel="stylesheet" href="assets/css/animate.min.css">
    <link rel="stylesheet" href="assets/css/aos.css">
    <link rel="stylesheet" href="assets/css/bootstrap.min.css">
    <link rel="stylesheet" href="assets/css/style.css">
    <link rel="stylesheet" href="assets/css/media-query.css">
</head>
```

- [ ] **Step 6: Create shared JS includes reference**

All pages include these at the bottom before `</body>`:

```html
<script src="assets/js/jquery-3.7.1.js"></script>
<script src="assets/js/bootstrap.bundle.min.js"></script>
<script src="assets/js/slick.min.js"></script>
<script src="assets/js/TweenMax.min.js"></script>
<script src="assets/js/aos.js"></script>
<script src="assets/js/plugins.js"></script>
<script src="assets/js/style.js"></script>
<script>AOS.init({ duration: 800, once: true, offset: 50 });</script>
```

- [ ] **Step 7: Commit shared component references**

```bash
git add -A
git commit -m "docs: document shared components for website rebuild"
```

---

## Task 2: Update Home Page (index.html)

**Files:**
- Modify: `index.html`

Update nav links, feature cards (5 cards for new pages), program cards, CTA buttons, and footer links. Keep hero section, visual design, and all existing styles intact.

- [ ] **Step 1: Update nav bar**

Replace the existing `<header>...</header>` block (lines 393-425 approximately) and the nav styles/script with the shared nav from Task 1 Step 2. Set الرئيسية link to `color:#ff5722` (active).

- [ ] **Step 2: Update feature cards section**

Replace the 4-card feature section with 5 cards. The section starts around line 466. Replace the `<div class="row g-4 mt-4">` contents with 5 cards using `col-lg-4 col-md-6` for the first row (3 cards) and centering the last 2:

Cards data:
1. `warmup.html` | `fas fa-fire` | الإحماء | "تسخين العضلات وتجهيز الجسم قبل التمرين لتجنب الإصابات وزيادة الأداء"
2. `workouts.html` | `fas fa-dumbbell` | جداول التمارين | "جداول 4/5/6 أيام + Push Pull Legs + تضخيم + جلوتس + تمارين منزلية مع فيديو لكل تمرين"
3. `cardio.html` | `fas fa-heartbeat` | الكارديو | "تمارين كارديو وتمارين البطن لحرق الدهون وتحسين اللياقة البدنية"
4. `nutrition.html` | `fas fa-utensils` | خطط التغذية | "جداول غذائية من 1200 إلى 2500 سعرة + كيتو دايت + جداول رمضانية"
5. `calculator.html` | `fas fa-calculator` | حاسبة السعرات | "احسب سعراتك اليومية والماكرو بدقة حسب هدفك ونشاطك البدني"

Each card keeps the same inline style pattern from the existing cards (gradient background, hover effects, icon circle, etc.).

- [ ] **Step 3: Update program cards section**

Update the 6 program cards (lines 534-622) to link to `workouts.html` instead of `exercises.html`. Update card titles and descriptions:

1. `fas fa-dumbbell` | جدول 4 أيام | "تقسيم الجسم العلوي والسفلي - مثالي للمبتدئين والمتوسطين"
2. `fas fa-fire` | جدول 5 أيام | "تقسيم أكثر تفصيلاً مع تركيز عالٍ على كل مجموعة عضلية"
3. `fas fa-bolt` | جدول 6 أيام | "للمتقدمين - حجم تدريبي مكثف مع تكرار عالٍ"
4. `fas fa-arrows-alt` | Push Pull Legs | "نظام دفع وسحب وأرجل - من أفضل أنظمة التقسيم لحرق الدهون"
5. `fas fa-fire-alt` | تضخيم | "برنامج زيادة الوزن والكتلة العضلية للبنات"
6. `fas fa-home` | تمرين منزلي | "تمارين مقاومة فعالة بوزن الجسم في البيت"

All link to `workouts.html`.

- [ ] **Step 4: Update CTA buttons**

Change `href="exercises.html"` to `href="workouts.html"` in the CTA section (around line 636). Change button text from "تصفح التمارين" to "ابدأ التمرين".

- [ ] **Step 5: Update footer**

Replace the footer section with the shared footer from Task 1 Step 4. The footer starts around line 658.

- [ ] **Step 6: Verify and commit**

Open `index.html` in browser, verify all links work and styles look correct.

```bash
git add index.html
git commit -m "feat: update home page with new navigation and content structure"
```

---

## Task 3: Create Warm-ups Page (warmup.html)

**Files:**
- Create: `warmup.html`

**Content source:** 1.pdf pages 4-5, 2.pdf page 5, جدول تمارين.pdf pages 4-5

- [ ] **Step 1: Create warmup.html with full page structure**

Create the complete page with: head, nav (الإحماء active), banner, tips section, cardio warm-up section, upper body warm-up section, lower body warm-up section, video modal, footer, JS includes.

**Page banner:**
```html
<section style="background:linear-gradient(135deg,rgba(0,0,0,0.7),rgba(0,0,0,0.85)),url('assets/images/common_img/home-header-bg.jpg');background-size:cover;background-position:center;padding:120px 0 80px;text-align:center;position:relative;">
    <div style="position:absolute;bottom:0;left:0;width:100%;height:4px;background:linear-gradient(90deg,#ff5722,#ff9800,#ff5722);"></div>
    <div class="container">
        <h2 style="font-size:48px;font-weight:900;color:#fff;margin-bottom:10px;">الإحماء والتسخين</h2>
        <p style="color:#ccc;font-size:16px;">الرئيسية / <span style="color:#ff5722;">الإحماء</span></p>
    </div>
</section>
```

**Tips section** - 3 cards in a row:
```html
<section style="padding:80px 0;background:#0a0a0a;">
    <div class="container">
        <div class="text-center mb-5">
            <div style="width:60px;height:3px;background:#ff5722;margin:0 auto 15px;border-radius:2px;"></div>
            <h3 style="color:#fff;font-weight:800;">نصائح قبل التمرين</h3>
        </div>
        <div class="row g-4">
            <div class="col-lg-4 col-md-6" data-aos="fade-up">
                <div style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:35px 25px;text-align:center;height:100%;">
                    <div style="width:70px;height:70px;background:linear-gradient(135deg,#ff5722,#ea580c);border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 20px;font-size:1.8rem;color:#fff;">
                        <i class="fas fa-tint"></i>
                    </div>
                    <h5 style="color:#fff;font-weight:700;margin-bottom:10px;">شرب الماء</h5>
                    <p style="color:#888;font-size:0.95rem;line-height:1.7;">تأكد من شرب كمية كافية من الماء قبل وأثناء التمارين الرياضية. البقاء مترطباً يساعد على الأداء الجيد ويساعد في منع الجفاف.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6" data-aos="fade-up" data-aos-delay="100">
                <div style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:35px 25px;text-align:center;height:100%;">
                    <div style="width:70px;height:70px;background:linear-gradient(135deg,#ff5722,#ea580c);border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 20px;font-size:1.8rem;color:#fff;">
                        <i class="fas fa-utensils"></i>
                    </div>
                    <h5 style="color:#fff;font-weight:700;margin-bottom:10px;">عدم التمرين على معدة فارغة</h5>
                    <p style="color:#888;font-size:0.95rem;line-height:1.7;">التمرين على معدة فارغة قد تسبب مشاكل كالشعور بالدوخة وانخفاض السكر بالدم. تناول وجبة خفيفة قبل التمرين بساعة.</p>
                </div>
            </div>
            <div class="col-lg-4 col-md-6" data-aos="fade-up" data-aos-delay="200">
                <div style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:35px 25px;text-align:center;height:100%;">
                    <div style="width:70px;height:70px;background:linear-gradient(135deg,#ff5722,#ea580c);border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 20px;font-size:1.8rem;color:#fff;">
                        <i class="fas fa-clock"></i>
                    </div>
                    <h5 style="color:#fff;font-weight:700;margin-bottom:10px;">أهمية التسخين</h5>
                    <p style="color:#888;font-size:0.95rem;line-height:1.7;">تسخين العضلات خطوة أساسية لتجنب الإصابات وزيادة الأداء. المدة: 5-10 دقائق. الهدف: رفع حرارة الجسم وزيادة تدفق الدم للعضلات.</p>
                </div>
            </div>
        </div>
    </div>
</section>
```

**Cardio warm-up section:**
```html
<section style="padding:60px 0;background:#111;">
    <div class="container">
        <div class="text-center mb-5">
            <div style="width:60px;height:3px;background:#ff5722;margin:0 auto 15px;border-radius:2px;"></div>
            <h3 style="color:#fff;font-weight:800;">التسخين بالكارديو</h3>
            <p style="color:#888;">ابدأ بالمشي على جهاز السير</p>
        </div>
        <div class="row justify-content-center g-4">
            <div class="col-lg-5 col-md-6" data-aos="fade-up">
                <div style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:30px;text-align:center;">
                    <div style="font-size:3rem;color:#ff5722;margin-bottom:15px;"><i class="fas fa-walking"></i></div>
                    <h5 style="color:#fff;font-weight:700;">المرحلة الأولى</h5>
                    <p style="color:#ff5722;font-size:1.5rem;font-weight:800;margin:10px 0;">10 دقائق</p>
                    <p style="color:#888;">المشي بسرعة بطيئة على جهاز السير لضخ الدم في كامل الجسم ورفع درجة حرارة الجسم</p>
                </div>
            </div>
            <div class="col-lg-5 col-md-6" data-aos="fade-up" data-aos-delay="100">
                <div style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:30px;text-align:center;">
                    <div style="font-size:3rem;color:#ff5722;margin-bottom:15px;"><i class="fas fa-running"></i></div>
                    <h5 style="color:#fff;font-weight:700;">المرحلة الثانية</h5>
                    <p style="color:#ff5722;font-size:1.5rem;font-weight:800;margin:10px 0;">5 دقائق</p>
                    <p style="color:#888;">المشي بسرعة متوسطة لزيادة تدفق الدم وتجهيز العضلات للتمرين</p>
                </div>
            </div>
        </div>
    </div>
</section>
```

**Upper/Lower body warm-up sections** with exercise cards. Each exercise card follows this pattern:

```html
<div class="col-lg-6 col-md-6" data-aos="fade-up">
    <div style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:25px;display:flex;flex-direction:column;gap:10px;">
        <h5 style="color:#fff;font-weight:700;margin:0;">المشي السريع أو الجري الخفيف</h5>
        <p style="color:#888;font-size:0.85rem;margin:0;">Brisk Walking or Light Jogging</p>
        <p style="color:#ccc;font-size:0.9rem;line-height:1.7;margin:0;">ابدأ بحركة خفيفة مثل المشي السريع أو الجري البطيء لتحريك الدورة الدموية</p>
        <div style="display:flex;gap:15px;align-items:center;">
            <span style="background:#222;padding:6px 14px;border-radius:6px;color:#ff5722;font-size:0.85rem;font-weight:600;">2-3 دقائق</span>
        </div>
    </div>
</div>
```

**Upper body warm-up exercises (from جدول تمارين.pdf page 4):**
1. المشي السريع أو الجري الخفيف | Brisk Walking or Light Jogging | 2-3 دقائق
2. تمرين Jumping Jacks | Jumping Jacks | 30 ثانية إلى دقيقة
3. القرفصاء مع تمديد الذراعين | Bodyweight Squats | 10-12 تكرار
4. تمرين Plank with Shoulder Taps | Plank with Shoulder Taps | 10 تكرارات لكل يد

**Lower body warm-up exercises:** Same structure, from PDF page 5 lower body section.

Include video modal and footer at bottom.

- [ ] **Step 2: Verify and commit**

Open `warmup.html` in browser, check layout and all sections render properly.

```bash
git add warmup.html
git commit -m "feat: create warm-ups page with tips, cardio and exercise warm-ups"
```

---

## Task 4: Create Workouts Page (workouts.html)

**Files:**
- Create: `workouts.html`

This is the largest page. It contains 7 tabs, each with a weekly schedule and daily exercise cards with video modal buttons.

**Content sources:**
- 1.pdf pages 8-27 (4/5/6-day splits)
- جدول تمارين.pdf pages 8-69 (تضخيم, PPL, جلوتس, منزلي)

- [ ] **Step 1: Create page skeleton with tab system**

Create `workouts.html` with head, nav (التمارين active), banner, tab bar, 7 tab panels (empty initially), video modal, footer, JS includes.

**Tab bar CSS + HTML:**
```html
<style>
    .workout-tabs { display:flex;gap:10px;overflow-x:auto;padding:20px 0;-webkit-overflow-scrolling:touch; }
    .workout-tabs::-webkit-scrollbar { height:4px; }
    .workout-tabs::-webkit-scrollbar-track { background:#111; }
    .workout-tabs::-webkit-scrollbar-thumb { background:#333;border-radius:2px; }
    .tab-btn { background:#1a1a1a;border:2px solid #333;color:#ccc;padding:10px 24px;border-radius:30px;cursor:pointer;font-family:'Tajawal',sans-serif;font-size:0.95rem;font-weight:600;white-space:nowrap;transition:all 0.3s; }
    .tab-btn:hover { border-color:#ff5722;color:#ff5722; }
    .tab-btn.active { background:#ff5722;border-color:#ff5722;color:#fff; }
    .tab-panel { display:none; }
    .tab-panel.active { display:block; }
    .day-header { background:#1a1a1a;border:1px solid #333;border-radius:10px;padding:15px 20px;cursor:pointer;display:flex;justify-content:space-between;align-items:center;margin-bottom:10px;transition:all 0.3s; }
    .day-header:hover { border-color:#ff5722; }
    .day-header.open { border-color:#ff5722;background:#222; }
    .day-content { display:none;padding:20px 0; }
    .day-content.open { display:block; }
    .exercise-card { background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:20px;transition:all 0.3s; }
    .exercise-card:hover { border-color:#ff5722; }
    .rest-card { background:linear-gradient(135deg,#1a1a1a,#111);border:2px solid #333;border-radius:12px;padding:40px;text-align:center; }
    .schedule-table { width:100%;border-collapse:separate;border-spacing:8px; }
    .schedule-table td { background:#1a1a1a;border-radius:8px;padding:15px 10px;text-align:center;color:#ccc;font-size:0.9rem; }
    .schedule-table td.workout-day { border:1px solid #ff5722;color:#ff5722; }
    .schedule-table td.rest-day { border:1px solid #333;color:#666; }
    .schedule-table th { color:#ff5722;padding:10px;font-size:0.85rem; }
</style>
```

**Tab switching JS:**
```javascript
function switchTab(tabId) {
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
    document.querySelector(`[data-tab="${tabId}"]`).classList.add('active');
    document.getElementById(tabId).classList.add('active');
}
function toggleDay(dayId) {
    const header = document.querySelector(`[data-day="${dayId}"]`);
    const content = document.getElementById(dayId);
    header.classList.toggle('open');
    content.classList.toggle('open');
}
```

- [ ] **Step 2: Populate Tab 1 - تمرين 4 أيام**

Source: 1.pdf pages 8-13. Weekly schedule + 7 day sections.

**Weekly schedule data:**
| اليوم | المحتوى |
|-------|---------|
| الأول | الجسم العلوي (صدر، كتف، تراي) |
| الثاني | الجسم السفلي (أرجل) |
| الثالث | راحة |
| الرابع | الجسم العلوي (ظهر، باي) |
| الخامس | الجسم السفلي (أرجل) |
| السادس | راحة |
| السابع | راحة |

**Day 1 exercises (page 10):**
1. ضغط الصدر العلوي | Incline Dumbbell Press | 4 جولات × 10 | video: `https://youtu.be/hciH2jeDXhs?si=HPQ1bZ2hm99FNVMo`
2. الدفع الصدر بالآلة | Chest Press Machine | 4 × 10 | video: `https://youtu.be/IlK0DxWs2Ug?si=LF2t1yXGIiOvV2at`
3. تفتيح الصدر | Machine Fly | 4 × 10 | video: `https://youtu.be/bomp27z-rXk`
4. الدفع أرنولد | Arnold Press | 4 × 10 | video: `https://youtu.be/wY00Zs2SNb4`
5. الدفع تراي | Push Downs | 4 × 10 | video: `https://youtube.com/shorts/S3scWsLHenk`
6. الرفرفة | Lateral Raise | 4 × 10 | video: `https://youtu.be/4oW78FD8YAs?si=SYiOO9Jo4KIcJz6G`

**Day 2 exercises (page 11):**
1. تمديد الفخذ | Leg Extensions | 4 × 10 | video: `https://youtube.com/shorts/tgVnKyJGTNA`
2. تمديد فخذ خلفي | Lying Leg Curls | 4 × 10 | video: `https://youtube.com/shorts/YE_ET_5B73s`
3. سكوات | Squats | 4 × 10 | video: `https://youtube.com/shorts/KJjBIaOSGuM`
4. بطات بالمقعد | Sitting Calf Raises | 4 × 10 | video: `https://youtube.com/shorts/MrYoG4FlJIk`
5. رفع الورك | Hip Thrust | 4 × 10 | video: `https://youtu.be/kCdQo5PQ5Ds?si=-MC3SqDyda98eyl2`
6. بلانك | Plank | 4 × 10 | video: `https://youtu.be/vUcc_2DnpqI`

**Day 3:** Rest day card with motivational message.

**Day 4 exercises (page 12):**
1. السحب العلوي | Pull-Down | 4 × 10 | video: `https://youtu.be/sbhjd9fvpKo?si=4TTVp9GZ9F0ChHve`
2. السحب الأرضي | Seated Cable Row | 4 × 10 | video: `https://youtu.be/5ZS33XfHnnE`
3. العقلة | Pull Up | 4 × قدر المستطاع | video: `https://youtube.com/shorts/TBYynYiKyk8`
4. تمديد الظهر | Back Extension | 4 × 10 | video: `https://youtu.be/Ue0SschQKOA?si=NTbnEmVVBQJnL41p`
5. كيبل لف | Cable Curl | 4 × 10 | video: `https://youtu.be/nrUM_lsMxbg`
6. لف الباي | Bicep Curl | 4 × 10 | video: `https://youtu.be/RFT0fRNNgdc`

**Day 5:** Same as Day 2 (page 13 repeats leg exercises).

**Day 6 & 7:** Rest day cards.

Each exercise card HTML:
```html
<div class="col-lg-6 col-md-6">
    <div class="exercise-card">
        <h5 style="color:#fff;font-weight:700;margin-bottom:5px;">ضغط الصدر العلوي</h5>
        <p style="color:#888;font-size:0.85rem;margin-bottom:10px;">Incline Dumbbell Press</p>
        <div style="display:flex;gap:15px;margin-bottom:15px;">
            <span style="background:#222;padding:5px 12px;border-radius:6px;color:#ff5722;font-size:0.85rem;">4 جولات</span>
            <span style="background:#222;padding:5px 12px;border-radius:6px;color:#ff5722;font-size:0.85rem;">10 تكرارات</span>
        </div>
        <button onclick="openVideoModal('https://youtu.be/hciH2jeDXhs?si=HPQ1bZ2hm99FNVMo')" style="background:#ff5722;border:none;color:#fff;padding:8px 20px;border-radius:6px;cursor:pointer;font-family:'Tajawal',sans-serif;font-size:0.9rem;">
            <i class="fas fa-play" style="margin-left:5px;"></i> شاهد الفيديو
        </button>
    </div>
</div>
```

- [ ] **Step 3: Populate Tab 2 - تمرين 5 أيام**

Source: 1.pdf pages 14-19. Same structure as Tab 1 but with 5 workout days. The exercises repeat with an additional lower body day (Day 5 = lower body).

Schedule: Day 1 upper, Day 2 lower, Day 3 upper (back/bi), Day 4 lower, Day 5 lower, Day 6-7 rest.

Uses the same exercise data from the 4-day split but spread across 5 days.

- [ ] **Step 4: Populate Tab 3 - تمرين 6 أيام**

Source: 1.pdf pages 20-27. 6 workout days with 1 rest day. Same exercise data distributed across 6 days.

- [ ] **Step 5: Populate Tab 4 - Push Pull Legs**

Source: جدول تمارين.pdf pages 20-35. PPL 4-day split for fat loss.

**Schedule:**
| Day | Content |
|-----|---------|
| 1 | Push: صدر وكتف وترايسبس وبطن |
| 2 | كارديو |
| 3 | Pull: ظهر وبايسبس وساعد وبطن |
| 4 | راحة |
| 5 | Legs: أرجل علوية وسفلية وقلوتس |
| 6 | راحة |
| 7 | راحة |

**Day 1 exercises (pages 23-24):**
1. صدر | Incline Smith Machine Bench Press | 5 × 8
2. ضغط صدر عالي دمبل | Dumbbell Incline Bench Press | 3 × 8
3. كتف جالس دامبل | Dumbbell Shoulder Press | 3 × 8
4. رفرفة كتف جانبي دامبل زوجي | Dumbbell Lateral Raise | 3 × 8
5. ترايسبس | Weighted Tricep Dips | 3 × 8
6. اوفر ترايسبس واقف حبل | Cable Rope Overhead Triceps Extension | 3 × 8
7. ضغط عضلات البطن | Ab Crunch Machine | 5 × 20

**Day 2 (page 26):**
1. جهاز الركض | Treadmill Running | 1 × 35 دقيقة

**Day 3 exercises (pages 28-29):**
1. عقلة باستخدام قبضة متوسطة | Pull Ups | 3 × 8
2. سحب بار مستوي واقف | Barbell Curl | 3 × 8
3. الرفعة الميتة بالبار | Barbell Deadlift | 3 × 8
4. سحب دامبل جالس منحني | Dumbbell Incline Curl | 3 × 8
5. جهاز سحب ظهر جالس قبضة متوسطة | Seated Machine Row | 3 × 8
6. مطرقة دامبل واقف | Dumbbell Hammer Curls | 3 × 8
7. تمرين القطنية على كرسي عالي | Hyperextensions - Back Extensions | 3 × 8
8. سحب عالي | Machine Lat Pull Down | 3 × 8

Video URLs from جدول تمارين.pdf hyperlinks for corresponding pages.

- [ ] **Step 6: Populate Tab 5 - تضخيم**

Source: جدول تمارين.pdf pages 8-19. Weight gain program.

**Schedule:**
| Day | Content |
|-----|---------|
| 1 | ظهر وكتف وبايسبس |
| 2 | صدر وكتف وترايسبس |
| 3 | راحة |
| 4 | بطن وأرجل علوية ومؤخرة (جلوتس) |
| 5 | أرجل علوية وسفلية |
| 6 | كارديو |
| 7 | راحة |

**Day 1 exercises (pages 10-11):**
1. الرفعة الميتة باستخدام جهاز السميث | Smith Machine Deadlift | 3 × 10
2. سحب ظهر امامي قبضة واسعة | Wide Grip Lat Pulldown | 3 × 10
3. تجديف باسخدام بار وجهاز السميث | Smith Machine Bent Over Row | 3 × 10
4. سحب كيبل حبل كتف خلفي واقف | Cable Rope Face Pull | 3 × 10
5. سحب دامبل يد واحدة (منشار) | Dumbbell One Arm Row | 3 × 10
6. سحب دامبل واقف | Dumbbell Bicep Curl | 3 × 10
7. تمرين القطنية على كرسي عالي | Hyperextensions - Back Extensions | 3 × 10
8. سحب كيبل جالس | Cable Seated Row | 3 × 10

Continue for all 7 days with exercises from corresponding PDF pages.

- [ ] **Step 7: Populate Tab 6 - جلوتس**

Source: جدول تمارين.pdf pages 56-60. Glutes-specific exercises.

No weekly schedule needed - single list of 9 exercises:

1. Barbell Glute Bridge | 4 × 8
2. Leg Over Knee Glute Bridge | 4 × 8
3. Hip Thrust | 4 × 8
4. Quadruped Kickbacks | 4 × 8
5. Step Ups | 4 × 8
6. Standing Kickbacks | 4 × 8
7. Hamstring Stability Ball | 4 × 8
8. Prone Hip Extension on the Stability Ball | 4 × 8
9. Clamshell | 4 × 8

Include anatomy info card at top about the 3 glute muscles (gluteus maximus, medius, minimus) from page 57.

- [ ] **Step 8: Populate Tab 7 - تمرين منزلي**

Source: جدول تمارين.pdf pages 61-69. Home workout program (bodyweight).

**Schedule:**
| Day | Content |
|-----|---------|
| 1 | مؤخرة (جلوتس) وبطن وأرجل علوية وظهر |
| 2 | صدر وترايسبس وظهر وبطن وكتف |
| 3 | راحة |
| 4 | أرجل علوية وبايسبس وترايسبس |
| 5 | ترايسبس وصدر وظهر وبطن |
| 6 | صدر وترايسبس وكتف وبطن |
| 7 | راحة |

**Day 1 exercises (pages 63-64):**
1. ركل الرجل للخلف | Glute Kickback | 3 × 10
2. لف الرجل اتجاه عكس الجسم | Iron Cross Stretch | 3 × 10
3. شد البطن مستلقي محاولة لمس القدم | Scorpion | 3 × 10
4. سكوات يد خلف الرأس | Prisoner Squat | 3 × 10
5. سكوات قفز بوزن الجسم | Freehand Jump Squat | 3 × 10
6. لف الرجل اتجاه عكس الجسم | Iron Cross Stretch | 3 × 10
7. تمرين سوبر مان | Superman | 3 × 10
8. اطالة البطن ساجد | Extended Arm Child Pose | 3 × 10

Continue for remaining days with corresponding PDF exercise data.

- [ ] **Step 9: Verify and commit**

Open `workouts.html` in browser. Test all 7 tabs switch correctly, day sections expand/collapse, video modals open and close.

```bash
git add workouts.html
git commit -m "feat: create workouts page with 7 tabbed programs and video modals"
```

---

## Task 5: Create Cardio Page (cardio.html)

**Files:**
- Create: `cardio.html`

**Content source:** جدول تمارين.pdf pages 6-7

- [ ] **Step 1: Create cardio.html**

Full page with: head, nav (الكارديو active), banner, cardio machines section (3 cards), ab exercises section (3 cards), video modal, footer, JS includes.

**Banner:** Same pattern as warmup, title "الكارديو".

**Cardio machines section - 3 cards:**

```html
<section style="padding:80px 0;background:#0a0a0a;">
    <div class="container">
        <div class="text-center mb-5">
            <div style="width:60px;height:3px;background:#ff5722;margin:0 auto 15px;border-radius:2px;"></div>
            <h3 style="color:#fff;font-weight:800;">تمارين الكارديو</h3>
            <p style="color:#888;">تمارين لحرق الدهون وتحسين اللياقة البدنية</p>
        </div>
        <div class="row g-4 justify-content-center">
            <div class="col-lg-4 col-md-6" data-aos="fade-up">
                <div class="exercise-card" style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:30px;text-align:center;">
                    <div style="font-size:3rem;color:#ff5722;margin-bottom:15px;"><i class="fas fa-running"></i></div>
                    <h5 style="color:#fff;font-weight:700;">جهاز الركض</h5>
                    <p style="color:#888;font-size:0.85rem;margin-bottom:5px;">Assault Runner</p>
                    <p style="color:#ff5722;font-size:1.2rem;font-weight:700;margin:10px 0;">10 دقائق على سرعة 7.0</p>
                    <button onclick="openVideoModal('VIDEO_URL_HERE')" style="background:#ff5722;border:none;color:#fff;padding:8px 20px;border-radius:6px;cursor:pointer;font-family:'Tajawal',sans-serif;font-size:0.9rem;margin-top:10px;">
                        <i class="fas fa-play" style="margin-left:5px;"></i> شاهد الفيديو
                    </button>
                </div>
            </div>
            <!-- Repeat for Elliptical Cross Trainer (10 min) and Assault Bike (10 min) -->
        </div>
    </div>
</section>
```

Machines:
1. جهاز الركض | Assault Runner | 10 دقائق على سرعة 7.0 | icon: `fa-running`
2. جهاز الكروس | Elliptical Cross Trainer | 10 دقائق | icon: `fa-bicycle`
3. الدراجة | Assault Bike | 10 دقائق | icon: `fa-biking`

**Ab exercises section - 3 cards:**

Same exercise-card pattern as workouts page:
1. آلة ضغط عضلات البطن | Ab Crunch Machine | 3 × 12
2. ضغط بطن عكسي على كرسي | Decline Reverse Crunch | 3 × 12
3. ضغط بطن مقلوب | Decline Crunch | 3 × 12

Video URLs from جدول تمارين.pdf page 6-7 hyperlinks.

Add the cardio page styles inline:
```css
.exercise-card { background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:20px;transition:all 0.3s; }
.exercise-card:hover { border-color:#ff5722; }
```

- [ ] **Step 2: Verify and commit**

```bash
git add cardio.html
git commit -m "feat: create cardio page with machines and ab exercises"
```

---

## Task 6: Rebuild Nutrition Page (nutrition.html)

**Files:**
- Modify: `nutrition.html`

**Content sources:**
- جدول تمارين.pdf pages 75-112 (calorie plans + keto)
- تغذية.pdf pages 1-43 (Ramadan)

- [ ] **Step 1: Rewrite nutrition.html with tab system**

Replace all existing content. Use shared head, nav (التغذية active), banner, tab bar (6 tabs), tab panels, footer, JS includes.

**Tab bar:** Same pill style as workouts page.

Tabs: 1200 سعرة | 1500 سعرة | 2000 سعرة | 2500 سعرة | كيتو دايت | رمضان

**Tab switching JS:** Same as workouts page `switchTab()` function with `ntab-` prefix to avoid ID conflicts if pages are merged.

- [ ] **Step 2: Populate 1200 calorie tab**

Source: جدول تمارين.pdf pages 78-85. Two variations (التشكيلة 1 / التشكيلة 2).

**Sub-tabs for variations:**
```html
<div style="display:flex;gap:10px;margin-bottom:30px;justify-content:center;">
    <button class="var-btn active" onclick="switchVariation('cal1200','v1')">التشكيلة 1</button>
    <button class="var-btn" onclick="switchVariation('cal1200','v2')">التشكيلة 2</button>
</div>
```

**Variation 1 meals (pages 78-81):**

Meal 1 - الإفطار:
- 3 بيضات مقلية + 2 شريحتين توست بر
- السعرات: 360.30 | البروتين: 24.75g | الكربوهيدرات: 25.22g | الدهون: 16.41g

Meal 2 - الغداء:
- صدور دجاج مشوية 150g + أرز أبيض مسلوق 100g + سلطة خضار 100g
- السعرات: 440.50 | البروتين: 50.99g | الكربوهيدرات: 41.36g | الدهون: 6.80g

Meal 3 - سناك قبل التمرين:
- تفاحة 100g + تمرة
- السعرات: 80.20 | البروتين: 0.50g | الكربوهيدرات: 21.31g | الدهون: 0.21g

Meal 4 - العشاء:
- سلطة تونة 150g + زبادي يوناني خ الدسم 150g
- السعرات: 367.50 | البروتين: 22.05g | الكربوهيدرات: 27.90g | الدهون: 21.00g

**Variation 2 meals (pages 82-85):**

Meal 1 - الإفطار:
- كورن فليكس 75g + كوب حليب ك الدسم 100g
- السعرات: 342 | البروتين: 8.33g | الكربوهيدرات: 67.27g | الدهون: 5.54g

Meal 2 - الغداء:
- صدر دجاج مشوي 200g + رز مسلوق 100g + سلطة خضار 130g
- السعرات: 561.70 | البروتين: 66.79g | الكربوهيدرات: 49.73g | الدهون: 8.88g

Meal 3 - سناك قبل التمرين:
- موزة + تمرة
- السعرات: 117.20 | البروتين: 1.33g | الكربوهيدرات: 30.34g | الدهون: 0.37g

Meal 4 - العشاء:
- لحم غنم 50g + مكرونة مسلوقة 50g
- السعرات: 204 | البروتين: 15.50g | الكربوهيدرات: 15.50g | الدهون: 7.00g

**Meal card HTML pattern:**
```html
<div class="col-lg-6 col-md-6" data-aos="fade-up">
    <div style="background:#1a1a1a;border:1px solid #333;border-radius:12px;padding:25px;height:100%;">
        <div style="display:flex;align-items:center;gap:10px;margin-bottom:15px;">
            <div style="width:45px;height:45px;background:linear-gradient(135deg,#ff5722,#ea580c);border-radius:10px;display:flex;align-items:center;justify-content:center;color:#fff;font-size:1.2rem;">
                <i class="fas fa-coffee"></i>
            </div>
            <h5 style="color:#fff;font-weight:700;margin:0;">الإفطار</h5>
        </div>
        <ul style="list-style:none;padding:0;margin:0 0 15px 0;">
            <li style="color:#ccc;padding:5px 0;border-bottom:1px solid #222;">3 بيضات مقلية</li>
            <li style="color:#ccc;padding:5px 0;">2 شريحتين توست بر</li>
        </ul>
        <div style="display:flex;flex-wrap:wrap;gap:10px;">
            <span style="background:#222;padding:5px 12px;border-radius:6px;font-size:0.8rem;"><span style="color:#ff5722;">360</span> <span style="color:#888;">سعرة</span></span>
            <span style="background:#222;padding:5px 12px;border-radius:6px;font-size:0.8rem;"><span style="color:#4ade80;">24.8g</span> <span style="color:#888;">بروتين</span></span>
            <span style="background:#222;padding:5px 12px;border-radius:6px;font-size:0.8rem;"><span style="color:#60a5fa;">25.2g</span> <span style="color:#888;">كربو</span></span>
            <span style="background:#222;padding:5px 12px;border-radius:6px;font-size:0.8rem;"><span style="color:#fbbf24;">16.4g</span> <span style="color:#888;">دهون</span></span>
        </div>
    </div>
</div>
```

Meal icons: الإفطار = `fa-coffee`, الغداء = `fa-drumstick-bite`, سناك = `fa-apple-alt`, العشاء = `fa-moon`

- [ ] **Step 3: Populate 1500 calorie tab**

Source: جدول تمارين.pdf pages 87-94. Same structure as 1200.

**Variation 1 meals (pages 87-90):**

Meal 1 - الإفطار:
- 2 بيضتين شكشوكة + 2 شريحتين توست بر + جبن سايل 50g
- السعرات: 366.80 | البروتين: 25.96g | الكربوهيدرات: 27.33g | الدهون: 15.44g

Meal 2 - الغداء:
- دجاج منزوع الجلد 150g + رز بني 100g + سلطة خضار 150g
- السعرات: 479.50 | البروتين: 51.81g | الكربوهيدرات: 48.73g | الدهون: 7.77g

Meal 3 - سناك قبل التمرين:
- تفاحة + تمرة
- السعرات: 80.20 | البروتين: 0.50g | الكربوهيدرات: 21.31g | الدهون: 0.21g

Meal 4 - العشاء:
- صدر دجاج 200g + أي نوع مكرونة 120g
- السعرات: 646 | البروتين: 72g | الكربوهيدرات: 62g | الدهون: 10g

**Variation 2 meals (pages 91-94):** Extract from PDF pages similarly.

- [ ] **Step 4: Populate 2000 calorie tab**

Source: جدول تمارين.pdf pages 96-103.

**Variation 1 meals (pages 96-99):**

Meal 1 - الإفطار:
- 4 بيضات مقلية + 4 شرائح توست + شرائح خيار 75g
- السعرات: 557.50 | البروتين: 38.49g | الكربوهيدرات: 48.35g | الدهون: 22.44g

Meal 2 - الغداء:
- صدر دجاج 200g + مكرونة مسلوقة 100g + خضروات + برتقالة 300g
- السعرات: 757 | البروتين: 76.27g | الكربوهيدرات: 87.21g | الدهون: 10.64g

Meal 3 - سناك:
- موزة + 2 تمر
- السعرات: 145.40 | البروتين: 1.58g | الكربوهيدرات: 37.85g | الدهون: 0.41g

Meal 4 - العشاء:
- 1 صب واي دجاج 6 انش + بطاطس مشوي 150g
- السعرات: 590 | البروتين: 26.45g | الكربوهيدرات: 77g | الدهون: 16g

**Variation 2 meals (pages 100-103):** Extract similarly.

- [ ] **Step 5: Populate 2500 calorie tab**

Source: جدول تمارين.pdf pages 105-112.

**Variation 1 meals (pages 105-108):**

Meal 1 - الإفطار:
- 4 بيضات مقلية + 4 شرائح توست + كوب حليب 150g
- السعرات: 647.50 | البروتين: 42.49g | الكربوهيدرات: 55.35g | الدهون: 26.44g

Meal 2 - الغداء:
- دجاج منزوع الجلد 200g + رز بني 150g + سلطة + كوب لبن 300g
- السعرات: 822 | البروتين: 74.96g | الكربوهيدرات: 95.72g | الدهون: 14.27g

Meal 3 - سناك:
- موزة + مكسرات مشكلة 35g
- السعرات: 304.25 | البروتين: 6.51g | الكربوهيدرات: 28.79g | الدهون: 20g

Meal 4 - العشاء:
- صدر دجاج 150g + باستا فوتشيني 200g
- السعرات: 646 | البروتين: 72g | الكربوهيدرات: 62g | الدهون: 10g

**Variation 2 meals (pages 109-112):** Extract similarly.

- [ ] **Step 6: Populate Keto tab**

Source: جدول تمارين.pdf pages 75-76. 7-day meal table.

```html
<div style="overflow-x:auto;">
    <table style="width:100%;border-collapse:separate;border-spacing:8px;min-width:600px;">
        <thead>
            <tr>
                <th style="color:#ff5722;padding:12px;text-align:center;">اليوم</th>
                <th style="color:#ff5722;padding:12px;text-align:center;">فطور</th>
                <th style="color:#ff5722;padding:12px;text-align:center;">غداء</th>
                <th style="color:#ff5722;padding:12px;text-align:center;">عشاء</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td style="background:#1a1a1a;border-radius:8px;padding:15px;text-align:center;color:#fff;font-weight:700;">السبت</td>
                <td style="background:#1a1a1a;border-radius:8px;padding:15px;color:#ccc;">بيض مخفوق</td>
                <td style="background:#1a1a1a;border-radius:8px;padding:15px;color:#ccc;">دجاج مشوي + سلطة صوص ليمون</td>
                <td style="background:#1a1a1a;border-radius:8px;padding:15px;color:#ccc;">سلطة خضار + علبة زبادي</td>
            </tr>
            <!-- Sunday through Friday rows -->
        </tbody>
    </table>
</div>
```

Keto 7-day data (from page 76):
| Day | فطور | غداء | عشاء |
|-----|------|------|------|
| السبت | بيض مخفوق | دجاج مشوي + سلطة صوص ليمون | سلطة خضار + علبة زبادي |
| الأحد | بيض مع افوكادو | سلطة متبلة بزيت زيتون مع صدر دجاج | سلطة خضار + تونه |
| الاثنين | شكشوكة جبن | سمك مشوي أو مقلي + سلطة خضار | زبادي يوناني + مكسرات |
| الثلاثاء | بيض مسلوق + شرائح خيار | شريحة لحم + سلطة خضار | سلطة خضار + تونه |
| الأربعاء | بيض مخفوق | شكشوكة جبن | دجاج مشوي + سلطة صوص ليمون |
| الخميس | بيض مسلوق + شرائح خيار | سمك مشوي أو مقلي + سلطة خضار | سلطة متبلة بزيت زيتون مع صدر دجاج |
| الجمعة | شكشوكة جبن | مشاوي + متبل | سلطة خضار + علبة زبادي |

- [ ] **Step 7: Populate Ramadan tab**

Source: تغذية.pdf pages 1-43. 1200 calorie Ramadan plans.

**Ramadan meals timing:** إفطار (Iftar), سناك, سحور

Extract meal data from تغذية.pdf. Two variations:

**Variation 1 (pages 3-4):**

الإفطار:
- 3 تمرات 25g + شوربة عدس 250g + 50g أرز مطبوخ + 50g دجاج مشوي + سلطة فتوش 150g + كوب لبن قليل الدسم 200g
- السعرات: 575 | البروتين: 41.2g | الكربوهيدرات: 86g | الدهون: 11.1g

سناك:
- كوب عصير تفاح طبيعي بدون سكر 250ml + حفنة مكسرات (لوز/كاجو) 20g
- السعرات: 240 | البروتين: 4.5g | الكربوهيدرات: 34g | الدهون: 10g

Continue with remaining meals from PDF.

- [ ] **Step 8: Verify and commit**

```bash
git add nutrition.html
git commit -m "feat: rebuild nutrition page with 6 tabbed meal plans"
```

---

## Task 7: Update Calculator and Contact pages (nav links only)

**Files:**
- Modify: `calculator.html`
- Modify: `contact.html`

- [ ] **Step 1: Update calculator.html nav**

Replace the nav bar in `calculator.html` with the shared nav from Task 1 Step 2. Set حاسبة السعرات link to `color:#ff5722` (active). Also update the footer links to match new pages.

Keep all calculator logic and styles intact.

- [ ] **Step 2: Update contact.html nav**

Replace the nav bar in `contact.html` with the shared nav from Task 1 Step 2. Set تواصل معنا link to `color:#ff5722` (active). Also update the footer links.

Keep all contact form logic and styles intact.

- [ ] **Step 3: Commit**

```bash
git add calculator.html contact.html
git commit -m "feat: update calculator and contact pages with new navigation"
```

---

## Task 8: Clean up old pages and push to GitHub

**Files:**
- Delete: `muscles.html`
- Delete: `equipment.html`
- Delete: `exercises.html`

- [ ] **Step 1: Delete old pages**

```bash
git rm muscles.html equipment.html exercises.html
```

- [ ] **Step 2: Final verification**

Open each page in browser and verify:
- [ ] `index.html` - hero, 5 feature cards, 6 program cards, CTA, footer links
- [ ] `warmup.html` - tips, cardio warm-up, upper/lower body exercises
- [ ] `workouts.html` - all 7 tabs work, days expand, video modals open
- [ ] `cardio.html` - 3 machines, 3 ab exercises, video modals
- [ ] `nutrition.html` - all 6 tabs, sub-tabs for variations, meal cards with macros, keto table
- [ ] `calculator.html` - nav updated, calculator works
- [ ] `contact.html` - nav updated, form works

- [ ] **Step 3: Commit and push to GitHub**

```bash
git add -A
git commit -m "chore: remove old pages (muscles, equipment, exercises)"
git remote add origin https://github.com/agamaldev/FIT.git
git branch -M main
git push -u origin main
```
