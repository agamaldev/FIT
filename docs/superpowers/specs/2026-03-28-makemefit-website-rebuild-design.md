# MAKE ME FIT Website Rebuild - Design Spec

## Overview

Replace all content on the existing MAKE ME FIT Arabic fitness website with content extracted from 4 PDF source files. Restructure pages to match the PDF content while keeping the existing visual design (dark theme, #ff5722 orange accent, Zen Dots + Tajawal fonts, Bootstrap, RTL Arabic).

**Source PDFs:**
- `Source/1.pdf` (47 pages) - Fitness program: warm-ups, 4/5/6-day gym splits, cardio, nutrition, home workouts
- `Source/2.pdf` (55 pages) - Fitness Factory program: weekly schedule, exercises per muscle group with sets/reps
- `Source/تغذية.pdf` (43 pages) - Ramadan nutrition plans (1200 cal)
- `Source/جدول تمارين .pdf` (112 pages) - Complete guide: warm-ups, cardio, push/pull/legs, full body, glutes, home workouts, nutrition plans (1200/1500/2000/2500 cal, keto), calorie calculator

## Site Structure

### Pages

| Page | File | Description |
|------|------|-------------|
| Home | `index.html` | Hero section + feature cards linking to pages + CTA |
| Warm-ups | `warmup.html` | Pre-workout warm-up guides |
| Workouts | `workouts.html` | Tabbed workout programs (7 tabs) |
| Cardio | `cardio.html` | Cardio machines + ab exercises |
| Nutrition | `nutrition.html` | Tabbed meal plans by calorie level |
| Calorie Calculator | `calculator.html` | Keep existing interactive calculator |
| Contact | `contact.html` | Keep existing placeholder contact form |

### Navigation

Same fixed-top nav bar pattern as current site:
- Dark background (#111), RTL direction
- Logo: "MAKE ME" (orange) + "FIT" (white) in Zen Dots font
- Desktop: horizontal links - الرئيسية | الإحماء | التمارين | الكارديو | التغذية | حاسبة السعرات | تواصل معنا
- Mobile: hamburger menu with slide-down links
- Active page link highlighted in #ff5722

### Pages Removed
- `muscles.html` - no equivalent in new PDF content
- `equipment.html` - no equivalent in new PDF content
- `exercises.html` - replaced by `workouts.html`

---

## Page Designs

### 1. Home Page (index.html)

**Hero Section** - Keep existing design:
- Background image with dark overlay
- "MAKE ME FIT" heading in Zen Dots
- "NO PAIN NO GAIN" tagline
- Arabic subtitle + CTA button to workouts page

**Features Section** - 5 cards (updated from current 4):
| Card | Icon | Link | Arabic Title |
|------|------|------|-------------|
| Warm-ups | fas fa-fire | warmup.html | الإحماء |
| Workouts | fas fa-dumbbell | workouts.html | التمارين |
| Cardio | fas fa-heartbeat | cardio.html | الكارديو |
| Nutrition | fas fa-utensils | nutrition.html | التغذية |
| Calculator | fas fa-calculator | calculator.html | حاسبة السعرات |

Each card: dark background (#1a1a1a), border (#333), orange hover effect, icon in gradient circle, title + short description.

**CTA Section** - Keep existing "ابدأ رحلتك" design with updated button links.

**Footer** - Updated quick links to match new page structure.

### 2. Warm-ups Page (warmup.html)

**Banner** - Page header with background image, title "الإحماء والتسخين", breadcrumb.

**Tips Section** - 3 tip cards at top with icons:
- شرب الماء (Drink water)
- عدم التمرين على معدة فارغة (Don't train on empty stomach)
- نصائح عامة (General pre-workout advice)
- Source: 1.pdf page 4

**Section 1: التسخين بالكارديو (Cardio Warm-up)**
- Walking on treadmill instructions: 10 min slow speed, then 5 min medium speed
- Source: 1.pdf page 5, 2.pdf page 5

**Section 2: تمارين الإحماء (Joint/Muscle Warm-up)**
Two sub-sections with toggle:
- **إحماء الجزء العلوي** (Upper body warm-up) - before chest/back/shoulders/arms
- **إحماء الجزء السفلي** (Lower body warm-up) - before leg days

Each lists exercises as cards:
- Exercise name (Arabic + English)
- Duration or reps
- Video button (opens YouTube modal)
- Source: جدول تمارين.pdf pages 4-5

### 3. Workouts Page (workouts.html) - Core Page

**Banner** - Title "جداول التمارين", breadcrumb.

**Tab Bar** - Horizontally scrollable pill buttons, orange (#ff5722) active state:

| Tab | Label | Source |
|-----|-------|--------|
| 1 | تمرين 4 أيام | 1.pdf pages 8-13 |
| 2 | تمرين 5 أيام | 1.pdf pages 14-19 |
| 3 | تمرين 6 أيام | 1.pdf pages 20-27 |
| 4 | Push Pull Legs | جدول تمارين.pdf pages 20-35 |
| 5 | تضخيم | جدول تمارين.pdf pages 8-19 |
| 6 | جلوتس | جدول تمارين.pdf pages 56-60 |
| 7 | تمرين منزلي | جدول تمارين.pdf pages 61-69 |

**Each tab contains:**

1. **Program description** - Brief intro text about the program goal (from PDF introductions)

2. **Weekly schedule table** - 7-column grid:
   - Day name (اليوم الأول through اليوم السابع)
   - Muscle group(s) targeted or "راحة" (rest)
   - Color-coded: orange for workout days, dark gray for rest days

3. **Day sections** - Each day is a collapsible/expandable section:
   - **Header:** Day name + muscle groups targeted
   - **Exercise cards grid** (2 columns desktop, 1 column mobile):
     - Exercise name in Arabic
     - Exercise name in English (smaller, gray)
     - Target muscle (الاستهداف)
     - Sets (المجموعات): number
     - Reps (التكرارات): number
     - Video button: "شاهد الفيديو" - opens YouTube modal popup
   - **Rest days** show motivational card: "الراحة جزء من التمرين مو وقت ضايع 💪"

### 4. Cardio Page (cardio.html)

**Banner** - Title "الكارديو", breadcrumb.

**Cardio Machines Section** - 3 cards in a row:
| Machine | Arabic | Duration | Source |
|---------|--------|----------|--------|
| Assault Runner | جهاز الركض | 10 min at speed 7.0 | جدول تمارين.pdf page 6 |
| Elliptical Cross Trainer | جهاز الكروس | 10 min | جدول تمارين.pdf page 6 |
| Assault Bike | الدراجة | 10 min | جدول تمارين.pdf page 6 |

Each card: machine name (Arabic + English), duration, video button (modal).

**Ab Exercises Section** - Title "تمارين البطن", 3 exercise cards:
| Exercise | Arabic | Sets | Reps | Source |
|----------|--------|------|------|--------|
| Ab Crunch Machine | آلة ضغط عضلات البطن | 3 | 12 | جدول تمارين.pdf page 7 |
| Decline Reverse Crunch | ضغط بطن عكسي على كرسي | 3 | 12 | جدول تمارين.pdf page 7 |
| Decline Crunch | ضغط بطن مقلوب | 3 | 12 | جدول تمارين.pdf page 7 |

Each card: same format as workout exercise cards with video button.

### 5. Nutrition Page (nutrition.html)

**Banner** - Title "التغذية", breadcrumb.

**Tab Bar** - Same pill style as workouts page:

| Tab | Label | Source |
|-----|-------|--------|
| 1 | 1200 سعرة | جدول تمارين.pdf pages 77-85 |
| 2 | 1500 سعرة | جدول تمارين.pdf pages 86-93 |
| 3 | 2000 سعرة | جدول تمارين.pdf pages 95-103 |
| 4 | 2500 سعرة | جدول تمارين.pdf pages 104-112 |
| 5 | كيتو دايت | جدول تمارين.pdf pages 75-76 |
| 6 | رمضان | تغذية.pdf pages 1-43 |

**Calorie tabs (1200/1500/2000/2500) each contain:**

Sub-tabs for variations: التشكيلة 1 | التشكيلة 2

Each variation shows 4 meal cards:

| Meal | Arabic |
|------|--------|
| Breakfast | الإفطار |
| Lunch | الغداء |
| Pre-workout Snack | سناك قبل التمرين |
| Dinner | العشاء |

Each meal card:
- Meal name + icon
- Food items list with quantities in grams
- Macros row: السعرات (calories), البروتين (protein), الكربوهيدرات (carbs), الدهون (fat)
- Clean table layout with orange accent numbers

**Keto tab:**
- 7-day meal table (Saturday through Friday)
- 3 columns: فطور (breakfast), غداء (lunch), عشاء (dinner)
- Simple text meals per cell
- Source: جدول تمارين.pdf pages 75-76

**Ramadan tab:**
- Meal plans designed for Ramadan timing (إفطار, سناك, سحور)
- Source: تغذية.pdf

### 6. Calculator Page (calculator.html)

Keep existing page as-is. No content changes.

### 7. Contact Page (contact.html)

Keep existing page as-is. Placeholder contact form with generic data.

---

## Video Modal Component

Shared across all pages. Triggered by "شاهد الفيديو" buttons.

**Behavior:**
- Click video button -> opens modal with dark overlay (rgba(0,0,0,0.85))
- Centered responsive YouTube iframe (16:9 aspect ratio, max-width 800px)
- Close button (X icon) positioned top-right of modal
- Click outside iframe to close
- ESC key to close
- Iframe src set dynamically from button's data-video attribute
- On close: stop video playback (remove iframe src)

**Implementation:** Vanilla JS, no library. Single modal element reused across all video buttons.

**Video URLs:** Extracted from PDF hyperlinks where available. Where PDFs reference videos without URLs, placeholder YouTube search links will be used.

---

## Technical Stack

- **No changes** to existing libraries: Bootstrap 5, AOS.js, Slick, jQuery, Animate.css
- **No new dependencies** added
- **Shared patterns:**
  - Inline header/footer HTML in each page (same as current approach)
  - Inline `<style>` blocks per page (same as current approach)
  - Tab switching: vanilla JS toggling display of tab panels
  - Video modal: shared inline JS component
- **Fonts:** Tajawal (Arabic body), Zen Dots (English branding)
- **Colors:** #ff5722 (primary orange), #111/#0a0a0a (backgrounds), #fff/#ccc/#888 (text hierarchy)
- **Direction:** RTL throughout, `lang="ar" dir="rtl"` on all pages
- **Responsive:** Bootstrap grid, mobile-first. Tab bars horizontally scrollable on mobile.

---

## Data Extraction Notes

The PDFs contain exercise data in a structured format that will be manually transcribed into HTML:
- Exercise names (Arabic + English)
- Sets and reps numbers
- Target muscle groups
- YouTube video links (where embedded in PDF hyperlinks)
- Meal plans with exact gram quantities and macro values

Some PDF text extraction has RTL rendering artifacts (reversed characters). The actual content meaning is clear and will be written correctly in the HTML.

---

## Out of Scope

- No backend / database
- No user authentication
- No progress tracking (the PDF has paper tracking sheets - not applicable to web)
- No exercise image assets (video links serve this purpose)
- No search functionality
- No dark/light mode toggle (stays dark)
