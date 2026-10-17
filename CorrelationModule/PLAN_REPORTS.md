# Correlation Module – Phase 2: Correlation Report

> Status: **APPROVED & IMPLEMENTED** (2026-10-17). See `CHANGES.md` section 7.
> Source: `requirement.md` → "Reports Requirements", slide 3 of `Corelation module.pptx`

---

## 1. What we are building

A **Correlation** report page (under the Reports menu) where the user:

1. Picks **Model** → **Parameter** (a global parameter from the Mapping Master) → **Date range** → **Generate**.
2. Sees the **parameter name** as a heading under the filters.
3. Sees **graph-type tabs** at the top: **X Chart** now, with more types later (Histogram, …).
4. Gets **one card per mapped item**, all on one page. Each card has:
   - A header: **Application name** · **mapped item** (e.g. *1D BIW · Area > Part > Checkpoint > Location (Gap)*) · **specification** (LSL / USL).
   - A small stats strip: n, Mean, Min, Max, **Out-of-spec count**.
   - An **X Chart (individual readings)**: reading points over audit date, with LSL/USL lines. **Out-of-spec points are drawn as red markers.**
   - A **Zoom** button that opens the same chart in a large pop-up modal, with the full ApexCharts zoom, pan and reset toolbar.
5. Can **export**:
   - **Excel**: a summary sheet plus one sheet per mapped item with all readings, LSL, USL and an out-of-spec flag.
   - **PDF**: the parameter, filters and every card with its chart image and stats, landscape, 2 cards per page.

**All cards share the same date axis**, so a spike on a given day lines up across 1D, Torque, EST and GD. This is the "co-relate" part.

---

## 2. Backend design: same registry pattern as Phase 1

The list of applications stays **data, not code**. Each application gets a second stored procedure, this one for **readings**:

```
MM_CR_Application  + Reading_SP_Name   (new column)
   1D_BIW → SP_CR_Get_Readings_1D       1D_TCF → SP_CR_Get_Readings_1D
   TORQUE → SP_CR_Get_Readings_Torque   EST/GD → SP_CR_Get_Readings_EST_GD
```

### Reading SP contract (every app must follow it)
| Input | |
|---|---|
| `@Leaves dbo.MM_CR_LeafList READONLY` | The mapped leaves (Leaf_ID + Leaf_Type), **all leaves of the app in one call** |
| `@From_Date DATE`, `@To_Date DATE` | The date range |

| Output column | Meaning |
|---|---|
| `Leaf_ID`, `Leaf_Type` | Which mapped leaf the reading belongs to |
| `Audit_Date` | X axis |
| `Reading` | **Plotted value** (1D: `Reading`; Torque/EST/GD: `Average`, see Q1) |
| `Reading1`, `Reading2`, `Reading3` | Optional. Shown in the tooltip and in Excel |
| `LSL`, `USL` | Spec limits **for that reading** (the spec can change over time) |

**To add a new application later:** write its reading SP and set `Reading_SP_Name`. No code change.

### How each app's SP is built (from your queries)
| App | Source tables (3-part names) | Leaf filter | Notes |
|---|---|---|---|
| 1D TCF / BIW | `[1D_Module]`: `MM_Vehicle_Audit`, `MM_Track_Sheet`, `MM_SpecificationMaster` | `sheet.Location_ID` = leaf **and** spec type matches Leaf_Type (`Gap` → `spec.Is_Gap = 1`, `Flush` → `spec.Is_Flushness = 1`) | So Gap and Flush readings land on separate cards |
| Torque | `[PQ_Torque_Audit]`: `MM_Audit_Sheet_Details`, `MM_Audit_Master`, `MM_ParameterMaster` | `sheet.Parameter_ID` = leaf | LSL/USL come from the parameter |
| EST / GD | `[PQ_EST_GD]`: `MM_Audit_Sheet_Datails`*, `MM_Audit_Master`, `MM_SpecificationDetails` | `sheet.Location_ID` = leaf | *table name spelled as in requirement.md, please confirm (Q6) |

All of them keep `Is_NA = 0`, filter `Audit_Date BETWEEN @From AND @To`, and join the leaf list as a TVP (one query per app, never per leaf).

### API (`MM_CR_Report` controller, in the same `CorrelationModule` folder)
| Method | Route | Returns |
|---|---|---|
| GET | `GetReport?crParameterId=&fromDate=&toDate=` | The parameter, plus one **series** per mapping: app name, display text, leaf type, latest LSL/USL, stats and points `[{Date, Reading, R1, R2, R3, LSL, USL, Is_Out_Of_Spec}]` |

The service groups the parameter's mappings by app, calls each app's reading SP **once** and splits the rows per leaf. Stats (mean, min, max, out-of-spec count) are computed in C#, so every app gets them the same way.

**Performance guards:**
- The date range is limited to **max 12 months** (Q4).
- Only `Is_Active` mappings are read.
- One SP call per application.
- If one app's SP fails, its cards show an error and **the other cards still load**.

---

## 3. Frontend: dedicated folder (`UI/src/app/Features/CorrelationModule/report/`)

```
report/
├── correlation-report.component.ts|html     page: filters, heading, tabs, card grid, export
├── chart-types.ts                           registry of graph tabs (X Chart now)
├── x-chart-card.component.ts|html           one card (header, stats, ApexCharts X chart, zoom)
└── report-export.service.ts                 Excel (exceljs) + PDF (pdfmake)
```

**Graph tabs:** `chart-types.ts` is a list like `[{ key: 'xchart', label: 'X Chart', component: XChartCard }]`. Adding Histogram later means one more card component and one line in the list.

**Design:** the same clean style as the new Mapping Master (white cards, slate text, indigo accent). On desktop the cards sit in a **2-column grid**, 1 column on smaller screens. Each card has a ~260 px chart with **Zoom ⤢** in the card header. The modal shows the chart at ~70 vh.

**Route and menu:** `/reports/correlation`, added under the existing Reports routes. A `07_Menu_Report.sql` script adds the "Correlation" submenu under Reports.

---

## 4. Files

| New | Edited |
|---|---|
| `CorrelationModule/SQL/07_Report.sql`: `Reading_SP_Name` column, the 3 reading SPs, menu row | `UI/.../Reports/reports.routes.ts`: +1 route |
| `API.../CorrelationModule/Controllers/MM_CR_ReportController.cs` | `Program.cs`: +1 `AddScoped` |
| `API.../CorrelationModule/Data/MM_CR_ReportDataService.cs` | |
| `API.../CorrelationModule/Models/MM_CR_ReportDtos.cs` | |
| `UI/.../CorrelationModule/report/*` | |

The build check will be the same as before (`dotnet build` + production `ng build`).

---

## 5. Decisions (answered 2026-10-17)

| # | Decision |
|---|---|
| Q1 | Torque / EST / GD: **Reading1, Reading2 and Reading3 are each plotted as a separate point** at the same audit date; NULL readings are skipped. 1D has one reading |
| Q2 | The chart shows **Reading, LSL and USL only**, with no UCL/LCL/X̄. Out-of-spec (outside LSL/USL) = red marker |
| Q3 | Spec change → **stepped** LSL/USL line; the values are labelled **only at the start and end** of the line |
| Q4 | Date filter presets: **Last 7 days, This month, Last month, Last 3 months, Custom range** (start + end date). Max 12 months |
| Q5 | **X Chart only**; tabs are ready for more types (`report/chart-types.ts`) |
| Q6 | `MM_Audit_Sheet_Datails` is used as spelled (DB-level spelling) |
| Q7 | No extra filters |
| Q8 | Files live under CorrelationModule; route `/correlation/correlationreport`, which you add as a submenu of the Reports menu |
