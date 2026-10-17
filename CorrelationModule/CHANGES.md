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
