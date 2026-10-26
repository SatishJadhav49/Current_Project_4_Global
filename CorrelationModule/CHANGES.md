# Correlation Module – Phase 1: Change List & Deployment Steps

Build checks done on the dev machine (no SQL there):
- `dotnet build` (API): **0 errors, 0 warnings** in the new files
- `ng build` (UI, production): **success, no warnings**

---

## 1. Files to copy

### New: SQL (`CorrelationModule/SQL/`)
| File | What it does |
|---|---|
| `01_Tables.sql` | 4 tables (`MM_CR_Application`, `MM_CR_Application_Column`, `MM_CR_Parameter`, `MM_CR_Parameter_Mapping`), filtered unique indexes, 2 table types |
| `02_Seed_Applications.sql` | 5 apps (1D BIW, 1D TCF, Torque, EST, GD) + their grid columns |
| `03_SPs_Mapping.sql` | `SP_CR_Get_Models`, `SP_CR_Get_Applications`, `SP_CR_Get_Parameters`, `SP_CR_Save_Parameter`, `SP_CR_Delete_Parameter`, `SP_CR_Get_Parameter_Mappings`, `SP_CR_Get_Application_Master_SP`, `SP_CR_Get_Leaf_Mappings`, `SP_CR_Save_Mappings`, `SP_CR_Unmap` |
| `04_SPs_Master_Data.sql` | `SP_CR_Get_Master_1D_BIW`, `_1D_TCF`, `_Torque`, `_EST`, `_GD` (from `query.sql`) |
| `06_Upgrade_Gap_Flush_Split.sql` | **Upgrade only** (DB already on 01–05): separate Gap / Flush rows for 1D. See section 6 |
| `05_Menu_Insert.sql` | Adds the **Mapping Master** submenu (`correlation/mapping-master`) under the Masters menu |

### New: API (`API_PQ_Global_Reporting/CorrelationModule/`)
- `Controllers/MM_CR_MappingController.cs`: routes under `api/MM_CR_Mapping/...`
- `Data/MM_CR_MappingDataService.cs`
- `Models/MM_CR_MappingDtos.cs`

### New: UI (`UI/src/app/Features/CorrelationModule/`)
- `correlation.routes.ts`, `correlation.model.ts`, `correlation.service.ts`
- `mapping-master/mapping-master.component.ts|html`: Screen 1 (Model, stats, parameter list, New/Rename dialog, Delete, Map)
- `parameter-mapping/parameter-mapping.component.ts|html`: Screen 2 (app tabs; one grid per app with **mapped rows pinned on top**, map/unmap from the same grid)

### Edited: shared files (small additions only)
| File | Change |
|---|---|
| `API_PQ_Global_Reporting/Program.cs` | +1 line: `AddScoped<...CorrelationModule.Data.MM_CR_MappingDataService>()` |
| `UI/src/app/app.routes.ts` | +1 route: `correlation` → `CorrelationRoutes` (with `AuthGuard`) |

---

## 2. Deployment order on the test system

1. **Make sure `PQ_Dashboard_Master.dbo.MM_Model (Model_ID, Model_Name)` exists and has data.**
2. Run the SQL scripts **in order**, `01` → `05`, on `PQ_Dashboard_Master`.
   - In `05_Menu_Insert.sql`, check `@Masters_Menu_ID`. By default the script picks the first active `MM_Menus` row whose LinkName contains "Master"; set it explicitly if that is wrong.
   - SQL Server **2016 SP1 or later** is required (`CREATE OR ALTER`).
3. Copy the API files, then build and publish as usual.
4. Copy the UI files, then build and deploy as usual.
5. Log in → Masters → **Mapping Master**.

## 3. Quick test checklist

- [ ] The Model dropdown lists models from `MM_Model`
- [ ] Add a parameter; adding the same name again in the same model shows "already exists"
- [ ] The same name **is** allowed in a different model
- [ ] Edit / Delete work (delete also removes that parameter's mappings)
- [ ] Map: every tab (1D BIW, 1D TCF, Torque, EST, GD) loads data for the selected model
- [ ] Select available rows → "Map N to parameter"; the rows move to the **Mapped** section at the top of the list
- [ ] The All / Mapped / Unmapped switch and the column Filters work
- [ ] A leaf mapped to another parameter shows "+ other" (hover for names) and can still be mapped
- [ ] Unmap via the ✕ on a row, or select mapped rows → "Unmap N"; the row returns to Available
- [ ] Back to the list → mapped counts per app are shown

## 4. Assumptions to verify on the real DB

| Assumption | Where to fix if wrong |
|---|---|
| Each source DB has `dbo.MM_Model (Model_ID, Model_Name)` | Model lookup line in each SP in `04_SPs_Master_Data.sql` |
| `[PQ_Torque_Audit].dbo.MM_ParameterMaster` has a `SORTORDER` column | `SP_CR_Get_Master_Torque` ORDER BY |
| `[1D_Module]` area table column is `Area_Name` (query.sql used `Area_name`, which works with case-insensitive collation) | `SP_CR_Get_Master_1D_*` |
| `MM_Sub_Menus` columns match the C# model (`ActionName, LinkName, Menu_ID, Sort_Order, Is_Active, Audit_Type_Id, Inserted_Date`) | `05_Menu_Insert.sql` |
| The DB login used by the API can read `[1D_Module]`, `[PQ_EST_GD]` and `[PQ_Torque_Audit]` | Grant SELECT to that login |

Each master SP can be tested by itself:
```sql
EXEC dbo.SP_CR_Get_Master_1D_BIW @Model_Name = N'<your model name>';
```

## 5. Adding a new application later (e.g. CMM): no code change

1. Create `SP_CR_Get_Master_CMM @Model_Name` returning `Leaf_ID, Leaf_Name, Source_Model_ID, Display_Text` + display columns.
2. Add the app row and its columns to `02_Seed_Applications.sql` and re-run it.
3. The new tab appears automatically.

---

## 6. Update 2026-10-17: Gap / Flush shown as separate rows (1D BIW / 1D TCF)

A 1D location measured for both Gap and Flush now appears as **two rows**, and each row is mapped separately.
A mapping is identified by `Source_Leaf_ID` + new column `Source_Leaf_Type` (`Gap` / `Flush`; NULL for other apps).

**On a database that already has 01–05 installed:** run only `06_Upgrade_Gap_Flush_Split.sql`, the **whole file** (F5 with nothing selected).
It is self-contained: it adds the column, rebuilds the indexes, converts existing 1D mappings, recreates the `MM_CR_LeafList` type, **and recreates the affected stored procedures** (`SP_CR_Save_Mappings`, `SP_CR_Get_Leaf_Mappings`, `SP_CR_Get_Parameter_Mappings`, `SP_CR_Get_Master_1D_BIW`, `SP_CR_Get_Master_1D_TCF`).
The last result grid is a verification list, and every row must show **OK**.

Existing 1D mappings are kept: Gap-only becomes Gap, Flush-only becomes Flush, and a Gap + Flush location becomes **both** mappings.
A fresh install only needs 01–05, which already include this change.

**Code files changed:**
- API: `CorrelationModule/Models/MM_CR_MappingDtos.cs`, `CorrelationModule/Data/MM_CR_MappingDataService.cs`
- UI: `correlation.model.ts`, `parameter-mapping/parameter-mapping.component.ts`

**Check:** for a location with both measures, open 1D BIW. You should see 2 rows (Gap, Flush). Map only Gap and confirm Flush stays Available.

---

## 7. Phase 2 (2026-10-17): Correlation Report

**SQL:** run `SQL/07_Report.sql` (whole file, F5) after 01–06. It adds `MM_CR_Application.Reading_SP_Name`, the reading SPs
`SP_CR_Get_Readings_1D`, `SP_CR_Get_Readings_Torque` and `SP_CR_Get_Readings_EST_GD`, and `SP_CR_Get_Report_Mappings`.
The last result grid must show **OK** for every application.

**Menu:** add the submenu under the **Reports** menu with **ActionName = `correlation/correlationreport`** (LinkName as you like, e.g. "Correlation Report").

**New files**
- API `CorrelationModule/`: `Controllers/MM_CR_ReportController.cs`, `Data/MM_CR_ReportDataService.cs`, `Models/MM_CR_ReportDtos.cs`
- UI `Features/CorrelationModule/report/`: `correlation-report.component.ts|html`, `x-chart.component.ts`, `report-export.service.ts`, `report.model.ts`, `date-presets.ts`, `chart-types.ts`

**Edited files**
| File | Change |
|---|---|
| `API_PQ_Global_Reporting/Program.cs` | +1 line `AddScoped<...MM_CR_ReportDataService>()` |
| `UI/.../CorrelationModule/correlation.routes.ts` | +1 route `correlationreport` |
| `UI/.../CorrelationModule/correlation.service.ts` | +`getReport()` |
| `UI/angular.json` | `pdfmake` added to `allowedCommonJsDependencies` (removes build warning only) |

**Test single SPs in SSMS** (Leaf type: `N'Gap'` / `N'Flush'` for 1D, NULL otherwise):
```sql
DECLARE @L dbo.MM_CR_LeafList;
INSERT INTO @L (Source_Leaf_ID, Source_Leaf_Type) VALUES (<Location_ID>, N'Gap');
EXEC dbo.SP_CR_Get_Readings_1D @Leaves = @L, @From_Date = '2026-09-01', @To_Date = '2026-10-17';
```

**Checklist**
- [ ] Reports → Correlation Report opens; Model → Parameter (unmapped parameters disabled) → preset / custom dates → Generate
- [ ] The parameter name and summary chips (items, readings, out of spec) appear under the filters
- [ ] One card per mapped item with the app name, item, Gap/Flush tag, spec and stats; all charts share the same date axis
- [ ] Torque / EST / GD show up to 3 points per audit (R1–R3 in the tooltip); empty readings are skipped
- [ ] Out-of-spec points are red; a spec change gives a stepped line with values only at the start and end
- [ ] The zoom icon opens the large chart with zoom / pan / reset
- [ ] Excel (summary + one sheet per item) and PDF (charts + stats) download

---

## 8. Report update (2026-10-17): parameter search, reading-sequence axis, Last N audits

**SQL:** **re-run `SQL/07_Report.sql`** (whole file). The 3 reading SPs get a new optional `@Last_N` parameter. When it is set, the dates are ignored and the
**last N audits of each mapped item** (with all their readings) are returned. Date mode works as before.

**What changed**
- **Parameter dropdown is searchable:** type to filter, Enter picks the first match, and parameters without a mapping are shown greyed out.
- **X axis = reading sequence.** Only dates that have data appear. Reading1–3 of one audit are separate, evenly spaced points, each labelled with
  the audit date (the date repeats), instead of sitting on one vertical line.
- **New period option "Last N audits"** with a number box (1–1000). The heading, Excel and PDF show "Last N audits".

**Files changed**
- SQL: `07_Report.sql`
- API: `Controllers/MM_CR_ReportController.cs`, `Data/MM_CR_ReportDataService.cs`, `Models/MM_CR_ReportDtos.cs`
- UI `report/`: `correlation-report.component.ts|html`, `x-chart.component.ts`, `report-export.service.ts`, `report.model.ts`, `date-presets.ts`
- UI: `correlation.service.ts`

**Test a SP in Last N mode:**
```sql
DECLARE @L dbo.MM_CR_LeafList;
INSERT INTO @L (Source_Leaf_ID, Source_Leaf_Type) VALUES (<Parameter_ID>, NULL);
EXEC dbo.SP_CR_Get_Readings_Torque @Leaves = @L, @Last_N = 10;   -- 10 audits, up to 30 points
```

---

## 9. Report update (2026-10-17): Histogram tab, stats bar removed

**No SQL / API change**: UI only.

- The card stats bar (n / Mean / Min / Max / Out of spec) is removed from the cards, the zoom pop-up and the PDF.
- New graph tab **Histogram** next to **X Chart**. The same cards are used, and only the chart inside switches.
  - Readings are grouped into equal-width bins (~√n bins, 5–20). The range always includes LSL / USL.
  - LSL / USL (latest spec of the period) are drawn as dashed vertical lines with labels.
  - Bins whose centre lies outside the spec are red; the count is shown above each bar (when ≤ 12 bins); the tooltip shows the bin range, count and %.
  - Zoom pop-up and PDF export follow the active tab (the PDF title says "X Chart" or "Histogram").

**Files**
- New: `report/histogram-chart.component.ts`, `report/report-chart.ts` (shared chart contract + colours)
- Changed: `report/chart-types.ts`, `report/x-chart.component.ts`, `report/correlation-report.component.ts|html`, `report/report-export.service.ts`

---

## 10. Report update (2026-10-17): period dropdown, averaged readings (X̄), standard histogram

**SQL:** **re-run `SQL/07_Report.sql`** (whole file).
- Torque and EST/GD now return **one point per audit = average of the non-NULL Reading1..3** (the X̄ chart), plus `Reading1..Reading3` for the tooltip and Excel.
- 1D returns `Reading1..3` as NULL (single reading, X chart). Last N mode is unchanged.

**API:** points carry `Reading1..3`; each series has `Is_Average` (true for averaged apps).

**UI**
- **Period** is a dropdown (Last 7 days, This month, Last month, Last 3 months, Custom range, Last N audits). Custom dates or "No. of audits" appear
  **in the same row**, followed by Generate; the "Showing: …" text is under the row.
- Each card has a small chart title: "X̄ Chart · Average of readings" (Torque / EST / GD) or "X Chart · Individual readings" (1D);
  on the Histogram tab: "Histogram with Normal Curve". The tooltip shows the average plus R1 / R2 / R3.
- **Histogram** follows the standard logic in `histogram.ts`:
  - group width tied to the tolerance (~10 groups inside USL−LSL, round step, ≤ 25 bars); Sturges rule when there is no tolerance;
  - the range covers LSL / USL; spec lines are drawn on the nearest group;
  - normal curve (mean, sample std-dev) scaled to frequency; colours #7cb5ec / #1976d2; **no red bars**.
- Excel item sheets: averaged apps show Average + Reading 1–3 columns.

**Files**
- SQL: `07_Report.sql`
- API: `Data/MM_CR_ReportDataService.cs`, `Models/MM_CR_ReportDtos.cs`
- UI `report/`: `histogram-chart.component.ts`, `x-chart.component.ts`, `correlation-report.component.ts|html`, `report-export.service.ts`, `report.model.ts`

---

## 11. Report update (2026-10-17): LSL / USL removed from chart headers

**UI only.** The LSL / USL text line is removed from the card header, the zoom pop-up header and the PDF card heading, because the values are already on the charts.
The Histogram tab legend now matches the chart (Frequency, Normal curve, LSL / USL).

**Files:** `report/correlation-report.component.ts|html`, `report/report-export.service.ts`

---

## 12. Images (2026-10-21): view the source application's image in Mapping and Report

Images are **not stored** in PQ_Dashboard_Master; they are read live from each application's `MM_Image_Master.FileContent`.

**SQL:** run `SQL/08_Images.sql` (whole file, F5) after 01–07. The last grid must show **OK** per application.
- Adds `MM_CR_Application.Image_SP_Name` (same registry pattern; a new app only needs its own image SP).
- `SP_CR_Get_Image_1D`: Location → Checkpoint → **Part_ID** → `[1D_Module].dbo.MM_Image_Master` (an image of the same audit type / model is preferred).
- `SP_CR_Get_Image_EST_GD`: by **Location_ID** in `[PQ_EST_GD].dbo.MM_Image_Master`.
- `SP_CR_Get_Image_Torque`: by **Parameter_ID** in `[PQ_Torque_Audit].dbo.MM_Image_Master`.
- `SP_CR_Get_Application_Image_SP`: lookup used by the API.

**API:** `GET api/MM_CR_Image/GetImage?appId=&leafId=` returns the image file itself (content type detected from the bytes: PNG, JPEG, GIF, BMP,
TIFF, WEBP, ICO, SVG; FileContent may be binary or base64 text) or **404** when the item has no image. It is cached by the browser for 10 min.

**UI**
- **Parameter Mapping:** new **Image** column with a view button on every row; it opens the full-screen viewer.
- **Correlation Report:** each card header shows a **thumbnail** (lazy loaded) next to the application and item name; click opens the viewer.
  Items without an image show a quiet placeholder.
- **Viewer:** full screen, the image is fitted to the screen (any size, desktop / mobile); close with ✕, Esc or a click outside.

**Files**
- New: `SQL/08_Images.sql`; API `CorrelationModule/Controllers/MM_CR_ImageController.cs`, `CorrelationModule/Data/MM_CR_ImageDataService.cs`;
  UI `CorrelationModule/shared/image-viewer.component.ts`, `CorrelationModule/shared/image-thumb.component.ts`
- Changed: `Program.cs` (+1 `AddScoped<...MM_CR_ImageDataService>()`), `correlation.service.ts` (+`imageUrl()`),
  `parameter-mapping/parameter-mapping.component.ts|html`, `report/correlation-report.component.ts|html`

**Test an image SP in SSMS:**
```sql
EXEC dbo.SP_CR_Get_Image_1D @Leaf_ID = <Location_ID>;   -- returns FileContent
```

---

## 13. Images (2026-10-21): icon only where an image exists

**SQL:** **re-run `SQL/08_Images.sql`** (whole file). Each image SP now has a **list mode** (`@Leaf_ID` NULL + `@Leaves`): it returns the
Leaf_IDs that have an image, so each application is checked in one query.

**API:** master-data rows get `Has_Image`, and report series get `Has_Image`. If the check fails, it just returns "no images" (logged), so the page still loads.

**UI:** the image button (Parameter Mapping) and the thumbnail (report card) appear **only when the item has an image**; otherwise nothing is shown.

**Files**
- SQL: `08_Images.sql`
- API: `Data/MM_CR_ImageDataService.cs`, `Data/MM_CR_MappingDataService.cs`, `Data/MM_CR_ReportDataService.cs`, `Models/MM_CR_ReportDtos.cs`
- UI: `parameter-mapping/parameter-mapping.component.ts|html`, `report/correlation-report.component.html`, `report/report.model.ts`, `correlation.model.ts`

---

## 14. Fix (2026-10-21): report thumbnail stayed a grey box

The thumbnail `<img>` was `loading="lazy"` **and** hidden with `display:none` until loaded. Browsers never fetch a lazy image that has no layout box,
so the request was never sent. It is now kept in the layout with `opacity-0` while loading. (The Mapping viewer was not affected: it does not lazy-load.)

**File:** `UI/.../CorrelationModule/shared/image-thumb.component.ts` (UI only)

---

## 15. Image viewer (2026-10-21): small images enlarged to fit the screen

The full-screen viewer now scales **every** image, small or large, to the largest size that fits the screen with its proportions kept
(small uploads are enlarged instead of staying tiny). It re-fits when the window is resized. The card thumbnails already scaled up to their box.

**File:** `UI/.../CorrelationModule/shared/image-viewer.component.ts` (UI only)

---

## 16. New application: CMM (2026-10-23)

**SQL only:** run `SQL/09_App_CMM.sql` (whole file, F5) after 01–08. **No API / UI change and no redeploy**; the CMM tab, grid and
report cards come from the registry. The last grid must show Master_SP = OK, Reading_SP = OK, Grid_Columns = 5.

- **Database:** `[PQ_CMM]` (no `Is_Active` on parameter / area, no `Is_NA` on the sheet; nothing filtered on them).
- **Mapping item = Parameter + Co-ordinate.** `Source_Leaf_ID` = `Parameter_ID`, `Source_Leaf_Type` = co-ordinate (the same column 1D uses for
  Gap / Flush). The spec table can repeat a parameter + co-ordinate; the grid shows each combination **once**.
- **Grid columns:** Side · Area · Parameter · Point No · Co-ordinate (all filterable). New tab added **after GD** (change `Sort_Order` to move it).
- **Readings:** `MM_Audit_Sheet_Details.Deviation`, one point per sheet row, matched on `Parameter_ID` + `Coordinate`. **LSL / USL come from the spec row
  of that reading** (`sheet.Specification_ID`), so a spec change shows as a step. Date range and Last N audits both work.
- **No images:** `Image_SP_Name` stays NULL, so no image icons for CMM.
- Model match is by `Model_Name` via `[PQ_CMM].dbo.MM_Model`, like the other apps.

**Test in SSMS**
```sql
EXEC dbo.SP_CR_Get_Master_CMM @Model_Name = N'<model name>';
DECLARE @L dbo.MM_CR_LeafList;
INSERT INTO @L (Source_Leaf_ID, Source_Leaf_Type) VALUES (<Parameter_ID>, N'<co-ordinate>');
EXEC dbo.SP_CR_Get_Readings_CMM @Leaves = @L, @Last_N = 10;
```

---

## 17. CMM (2026-10-23): CTQ column + ROW_ID fix kept in the script

**SQL only:** **re-run `SQL/09_App_CMM.sql`** (whole file). No API / UI change. The verification grid now shows **Grid_Columns = 6**.

- **ROW_ID fix:** the spec table key is `ROW_ID`, so readings join `spec.ROW_ID = sheet.Specification_ID`. This was fixed directly in SQL; it is now also in
  the script, so a re-run keeps it.
- **CTQ (Critical To Quality), view only:** the CMM master SP returns a `CTQ` column per Parameter + Co-ordinate: **"CTQ"** when `IS_CTQ = 1`
  on any of its spec rows (the spec table can repeat the combination), empty otherwise. A **CTQ** grid column (filterable) is registered, so it
  shows on the Mapping page after Co-ordinate. Type "CTQ" in its column filter to list only critical points.

---

## 18. New application: Master Checker (2026-10-26)

**SQL only:** run `SQL/10_App_Master_Checker.sql` (whole file, F5) after 01–09. **No API / UI change and no redeploy.**
The last grid must show Master_SP = OK, Reading_SP = OK, Grid_Columns = 4.

- **Database:** `[PQ_Master_Checker]`; models matched by name via `MM_Model`; only active parts / locations.
- **Grid columns:** Part · Side · Location · Gap / Flush (all filterable). New tab after CMM (change `Sort_Order` to move it).
  A part with **Is_LH and Is_RH** gives **separate LH and RH rows** (and Common when Is_Common); a location with Is_Gap and Is_Flush gives
  separate Gap and Flush rows.
- **Mapping item:** `Location_ID` + type **"Side / Measure"**, e.g. `LH / Gap`, `Common / Flush` (stored in `Source_Leaf_Type`; also shown as the
  tag on report cards). Only the side (e.g. `RH`) when the location has no Gap/Flush flag; only the measure when the part has no side flag.
- **Readings:** `MM_Audit_Sheet_Details.Reading` (single reading, X chart), `Is_NA <> 1`, LSL / USL from the reading's own spec (`Spec_ID`).
  Side is matched on **`sheet.Side`** (text LH / RH / Common), Gap / Flush on **`spec.Is_Gap` / `spec.Is_Flush`**. Date range and Last N both work.
- **Images:** stored in an application server folder, **not added yet** (`Image_SP_Name` NULL, no image icons).

**Test in SSMS**
```sql
EXEC dbo.SP_CR_Get_Master_Master_Checker @Model_Name = N'<model name>';
DECLARE @L dbo.MM_CR_LeafList;
INSERT INTO @L (Source_Leaf_ID, Source_Leaf_Type) VALUES (<Location_ID>, N'LH / Gap');
EXEC dbo.SP_CR_Get_Readings_Master_Checker @Leaves = @L, @Last_N = 10;
```

---

## 19. Report + Mapping (2026-10-26): every date on the X axis, hide charts, custom application order

**SQL:** run `SQL/11_App_Order.sql` (whole file, F5) after 01–10.
- New table `MM_CR_Parameter_App_Order` (parameter + application + sort order) and table type `MM_CR_OrderList`.
- `SP_CR_Get_Parameter_App_Order` / `SP_CR_Save_Parameter_App_Order` (saving an empty list = back to default).
- `SP_CR_Get_Applications` (optional `@CR_Parameter_ID`) and `SP_CR_Get_Report_Mappings` now follow the parameter's custom order; the same
  versions are kept in `03_SPs_Mapping.sql` / `07_Report.sql`, so re-running those keeps it.

**1. X axis shows every date:** one tick per reading, so 20 points print 20 dates (rotated; the font gets smaller above 40 points).

**2. Hide charts (this view only):** each report card has a **hide** (eye) button, and a **Charts n / total** dropdown next to the graph tabs lists all
charts with checkboxes (Show all / Hide all). Hidden charts are left out of Excel / PDF too. Nothing is saved: a new Generate or a page
refresh shows all charts again.

**3. Custom application order per parameter:** Parameter Mapping page → **Application order** button → move applications up / down →
**Save order** (or **Reset to default**). The tabs on that page and the **report charts** follow this order for that parameter only; other
parameters keep the default order.

**Files**
- SQL: new `11_App_Order.sql`; changed `03_SPs_Mapping.sql`, `07_Report.sql`
- API: `Controllers/MM_CR_MappingController.cs` (`GetApplications?crParameterId=`, `GetParameterAppOrder/{id}`, `SaveParameterAppOrder`),
  `Data/MM_CR_MappingDataService.cs`, `Models/MM_CR_MappingDtos.cs`
- UI: `correlation.model.ts`, `correlation.service.ts`, `report/x-chart.component.ts`, `report/correlation-report.component.ts|html`,
  `parameter-mapping/parameter-mapping.component.ts|html`
