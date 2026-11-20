# Public UI redesign

Completed 4 October 2026 in the existing ASP.NET Core MVC / .NET 10 application.

## Design

- Original DiveMarketplace wordmark and inline ocean-inspired SVG mark replace the global Dive Hub logo. Dive Hub remains a normal marketplace supplier.
- Ocean teal, deep navy, warm neutral backgrounds and white; system UI body text with an editorial Georgia heading stack. No external font dependencies.
- Immersive home hero, destination discovery from actual active activity/center locations, current catalog countries in search, experience categories and partner invitation.
- Photograph-led experience and center cards, cover images kept distinct from center logos, genuine prices, duration and location information.
- Editorial activity and center details, desktop sticky reservation panel, responsive multi-image gallery with all real gallery photos available in a Bootstrap modal.
- Account pages, owner application form, registration success and the full customer reservation journey share the marketplace identity.
- Customer reservation estimate starts with a Razor-rendered total and updates with a small independent script. Existing server price snapshots and validation remain authoritative.
- Existing notification script retained. Skip link, labels, focus states, accessible gallery controls, semantic page sections and reduced-motion rules retained or added.
- CSS reorganized into foundation, navigation, hero/search, destinations, travel cards, categories, listings, details, reservations, forms, footer, management and responsive sections.

## Files changed

- `Controllers/HomeController.cs` — presentation-only grouping of existing public catalog data into real destinations; no additional database queries.
- `ViewModels/MarketplaceViewModels.cs` — destination presentation model and home destination collection.
- `Views/Shared/_Layout.cshtml` — branding, header, footer, skip link and separate public/management page classes; management navigation still appears on operational pages and role links remain accessible from the header.
- `Views/Shared/_Layout.cshtml.css` — removed unused default ASP.NET styling.
- `Views/Home/Index.cshtml`
- `Views/Activities/Index.cshtml`
- `Views/Activities/Details.cshtml`
- `Views/DiveCenters/Details.cshtml`
- `Views/Shared/_ActivityCards.cshtml`
- `Views/Shared/_DiveCenterCards.cshtml`
- `Views/Shared/_PhotoGallery.cshtml`
- `Views/Shared/_LocationMap.cshtml`
- `Views/Account/Login.cshtml`
- `Views/Account/Register.cshtml`
- `Views/Account/RegisterOwner.cshtml`
- `Views/Account/RegistrationSuccess.cshtml`
- `Views/Reservations/Create.cshtml`
- `Views/Reservations/Index.cshtml`
- `Views/Reservations/Details.cshtml`
- `Views/Shared/_CustomerReservationCards.cshtml` — new reusable customer trip cards; manager reservation tables remain unchanged.
- `wwwroot/css/site.css`
- `wwwroot/js/site.js` — added only the reservation estimate/date/participant summary script.
- `PUBLIC_UI_REDESIGN.md` — this report.

The Dive Centers listing inherits the new shared center cards, page headings and global design without changes to its view file.

## Intentionally unchanged

Database schema, SQL scripts, Dapper services, authentication and authorization, ownership checks, reservation workflow and price persistence. No payment, fake reviews, rating metrics, favorites, random image URLs, downloaded commercial photography or frontend framework added. Admin and center management views remain functional dashboard screens; their global styles receive small consistency updates. Existing account profile and owner application status functionality remains intact.

## Verification

- Final `dotnet build`: succeeded, 0 warnings, 0 errors. SDK emits its informational preview-SDK notice.
- Public HTTP 200 checks: Home, activity listing, both current activity detail pages, filtered activity listing, center listing/detail, login, customer signup and owner signup.
- Anonymous reservation request redirects to login with its return URL.
- Existing `Tests/SmokeTest.ps1`: 57 checks passed against the final build.
- Existing `Tests/PhotoSmokeTest.ps1`: 38 checks passed, including gallery output, cover selection, batch uploads, validation and ownership controls.
- Existing `Tests/LocationContactSmokeTest.ps1`: 21 checks passed, including maps, contact preferences, reservation snapshots and confirmation.
- Existing `Tests/ProfileManagementSmokeTest.ps1`: 26 checks passed, including management navigation and live notifications.
- Total: 142 existing regression checks passed. Their built-in cleanup was used. Existing test workflows retain inactive reservation audit records and their parents; this behavior was not changed.
- CSS breakpoint review covers 1199, 991, 767 and 575px widths; desktop sticky panels switch to normal document flow below 992px. Grid children and forms are constrained to avoid overflow. Reduced-motion overrides retained.
- **Visual verification limitation:** this session exposed no available browser. Real screenshot review, touch/keyboard interaction and measured mobile/tablet overflow checks could not be performed. Responsive rules were inspected, and pages were checked over HTTP; these are not substitutes for a final visual browser pass.

## Real photography to add

The existing public catalog currently has no suitable hero/activity photography, so intentional ocean-toned fallbacks are shown. Add photos through the existing management tools:

1. Activities: a high-quality landscape underwater cover for each experience. This automatically feeds activity cards, detail pages, reservation summaries, relevant destination/category cards and the home hero when available.
2. Dive centers: landscape covers showing the boat, shore, team or underwater environment; keep square/transparent logos separate.
3. Galleries: a variety of genuine wide and close-up photos. The first/cover image receives the large gallery position; all additional photos remain available in the modal.
4. Instructors: consistent real portraits for the team section.

Recommended hero/gallery cover shape: approximately 16:9, at least 1600px wide, with room for cropping. Keep important subjects away from the extreme edges. The home hero text overlays the left side, so a quieter left composition works best. Use images that the center owns or has permission to publish.
