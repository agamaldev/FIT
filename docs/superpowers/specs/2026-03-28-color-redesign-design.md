# MAKE ME FIT Color Scheme Redesign - Design Spec

## Overview

Replace the existing orange (#ff5722) color scheme with two new themes:
- **Golden Power (#f59e0b)** - all pages and men's workout tabs
- **Crimson Night (#e94560)** - women's workout tabs (PPL, تضخيم, جلوتس, تمرين منزلي)

No layout, spacing, font, or structural changes. Colors only.

## Color Mapping

### Golden Power (replaces orange on all pages)

| Current | New Golden |
|---------|-----------|
| #ff5722 (primary) | #f59e0b |
| #ff572233 (border alpha) | #f59e0b33 |
| #ff7043 (gradient end) | #d97706 |
| #ea580c (gradient end) | #d97706 |
| #ff6600 (banner accent) | #f59e0b |
| #ff9800 / #ff9500 (banner gradient) | #d97706 |
| #f97316 (used in index.html styles) | #f59e0b |
| #fb923c (gradient end) | #d97706 |
| rgba(255,87,34,0.15) (shadows) | rgba(245,158,11,0.15) |
| rgba(249,115,22,0.15) (shadows) | rgba(245,158,11,0.15) |
| rgba(249,115,22,0.08) (glow) | rgba(245,158,11,0.08) |

### Crimson Night (women's tabs in workouts.html)

| Property | Value |
|----------|-------|
| Primary | #e94560 |
| Gradient end | #c23152 |
| Border alpha | #e9456033 |
| Shadow | rgba(233,69,96,0.15) |

## Pages Affected

### All pages - Golden Power replacement:
1. **index.html** - nav, hero subtitle, feature cards, program cards, CTA, footer
2. **warmup.html** - nav, banner, tip icons, phase cards, exercise badges
3. **workouts.html** - nav, banner, tab bar, schedule table, exercise cards, video buttons (base theme)
4. **cardio.html** - nav, banner, machine cards, ab exercise cards, video buttons
5. **nutrition.html** - nav, banner, tab bar, variation buttons, meal card macros
6. **calculator.html** - nav, banner, form accents, buttons, results
7. **contact.html** - nav, banner, form accents, cards

### workouts.html - Dynamic theme switching:
- Add CSS custom properties (--primary, --primary-gradient, --primary-alpha, --primary-shadow)
- Default values: Golden Power
- On tab switch to PPL/تضخيم/جلوتس/تمرين منزلي: update CSS variables to Crimson Night
- On tab switch to 4 أيام/5 أيام/6 أيام: revert to Golden Power
- Add CSS transition on color properties for smooth switch

## What Does NOT Change

- Page layouts and structure
- Fonts (Tajawal, Zen Dots)
- Images and backgrounds
- Bootstrap classes
- JavaScript functionality (tabs, days, video modal)
- Video links
- Content text
- Spacing, padding, margins
- Border radius values
- Animation libraries (AOS)

## Implementation Approach

For each HTML file:
1. Find-and-replace all orange color values with Golden Power equivalents
2. For workouts.html additionally: refactor inline color references to use CSS custom properties, add switchTab theme logic

No new files created. No files deleted. Only existing files modified.
