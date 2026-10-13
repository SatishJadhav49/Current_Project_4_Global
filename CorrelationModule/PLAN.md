# Correlation Module – Phase 1: Parameter Mapping Master

> Status: **APPROVED & IMPLEMENTED** (2026-10-13). See `CHANGES.md` for files and deployment steps.
> Sources: `requirement.md`, `query.sql`, `Corelation module.pptx`, `Corelation_Module.xlsx`

---

## 1. What we are building (Phase 1 scope)

A **Mapping Master** where a user:

1. Picks a **Model**.
2. Creates a **Global Parameter** (a name that is unique within that model), e.g. *"Panoramic sunroof to headliner : 13-23 RH"*.
3. Opens that parameter and **maps leaf items from other applications** to it:
   - A top tab bar lists the applications (1D BIW, 1D TCF, CMM, Torque, EST, GD).
   - Each tab shows that application's master data for the model in a table (columns depend on the application).
   - The user selects rows and clicks **Map**. We store the application plus the **leaf ID** of each row.
4. Sees everything mapped to the parameter and can **unmap** it.

**Out of scope for Phase 1:** readings, X Chart, Histogram, the Reports page (slide 3). The tables are designed so Phase 2 can add these without changing any schema.

### Application hierarchy (from the Excel sheet and query.sql)

| Application | Hierarchy shown in grid | Leaf (ID we store) |
|---|---|---|
| 1D TCF | Area > Part > Checkpoint > Location | `Location_ID` |
| 1D BIW | Area > Part > Checkpoint > Location | `Location_ID` |
| CMM | Area > Parameter | `Parameter_ID` *(no query given yet)* |
| Torque Audit | Parameter | `Parameter_ID` |
| EST | Parameter > Location | `Location_ID` |
| GD | Parameter > Location | `Location_ID` |

---

## 2. Core design idea: the list of applications is data, not code

Requirement #4 says applications may be added or removed later. So **no application name is hardcoded** anywhere in the C# or Angular code.

- A registry table, **`MM_CR_Application`**, holds one row per application. Each row stores the app's name, leaf label and sort order, plus the **name of the stored procedure that returns its master data**.
- A child table, **`MM_CR_Application_Column`**, says which columns the grid should show for that app, with their headers and order.
- Every per-app master SP returns **one common shape**: `Leaf_ID`, `Leaf_Name`, plus any display columns. The API passes rows through as a dictionary, and the UI builds the table columns from the column metadata.

**To add a new application later:** write its master SP, then insert 1 row in `MM_CR_Application` and N rows in `MM_CR_Application_Column`. **No C# or Angular change and no redeploy.**
**To remove one:** set `Is_Active = 0`. Its mappings stay in the database for history.

```
UI tab bar  ──GET Applications──►  MM_CR_Application (active, sorted)
UI grid     ──GET MasterData(appId, modelId)──►  API looks up Master_SP_Name ──► EXEC that SP
                                                 (SP name comes from DB only, never from client)
```

---

## 3. Database design (in `PQ_Dashboard_Master`)

Every new object gets the `MM_CR_` prefix (CR = Correlation) so it is easy to spot and monitor.
The audit columns follow the existing `MM_` table convention: `Inserted_* / Updated_*`, `Is_Active`.

### 3.1 `MM_CR_Application` – application registry
| Column | Type | Notes |
|---|---|---|
| App_ID | INT IDENTITY PK | |
| App_Code | VARCHAR(30) UNIQUE | `1D_TCF`, `1D_BIW`, `CMM`, `TORQUE`, `EST`, `GD` |
| App_Name | NVARCHAR(100) | Tab label |
| Leaf_Level_Name | NVARCHAR(50) | `Location` / `Parameter`: the label for the stored ID |
| Master_SP_Name | SYSNAME | e.g. `SP_CR_Get_Master_1D_BIW` |
| Sort_Order | INT | Tab order |
| Is_Active | BIT | |
| audit columns | | |

### 3.2 `MM_CR_Application_Column` – grid column metadata
| Column | Type | Notes |
|---|---|---|
| App_Column_ID | INT IDENTITY PK | |
| App_ID | INT FK | |
| Column_Key | VARCHAR(100) | Column name returned by the SP, e.g. `Area_Name` |
| Column_Header | NVARCHAR(100) | e.g. `Area` |
| Sort_Order | INT | |
| Is_Filterable | BIT | Shows a filter box in the grid |
| Is_Active | BIT | |

### 3.3 `MM_CR_Parameter` – global parameter
| Column | Type | Notes |
|---|---|---|
| CR_Parameter_ID | INT IDENTITY PK | |
| Model_ID | DECIMAL | Selected model |
| Parameter_Name | NVARCHAR(250) | |
| Is_Active | BIT | Soft delete |
| audit columns | | |

**Uniqueness:** a filtered unique index on `(Model_ID, Parameter_Name) WHERE Is_Active = 1`. The SP also checks this first and returns a friendly message.

### 3.4 `MM_CR_Parameter_Mapping` – the mapping
| Column | Type | Notes |
|---|---|---|
| Mapping_ID | INT IDENTITY PK | |
| CR_Parameter_ID | INT FK → MM_CR_Parameter | |
| App_ID | INT FK → MM_CR_Application | |
| Source_Model_ID | DECIMAL | Model ID **in the source app** (see question Q1) |
| Source_Leaf_ID | DECIMAL | Location_ID / Parameter_ID in the source app |
| Source_Display_Text | NVARCHAR(1000) | Snapshot, e.g. `Area > Part > Checkpoint > Location`, so the "Mapped" list is fast and still readable if the source row is later deactivated |
| Is_Active | BIT | Unmap sets this to 0 (keeps history for tracking) |
| audit columns | | Who mapped or unmapped, and when |

**Duplicate protection (two levels):**
- The same leaf cannot be mapped twice to the same parameter: unique on `(CR_Parameter_ID, App_ID, Source_Leaf_ID) WHERE Is_Active = 1`.
- One leaf **can** be mapped to several different parameters (Q2). The UI flags it with "Also in other".

Phase 2 (readings) only needs `MM_CR_Parameter_Mapping` → `App_ID + Source_Leaf_ID` to pull readings from each application. That is the reason we store the leaf ID.

### 3.5 Stored procedures
| SP | Purpose |
|---|---|
| `SP_CR_Get_Applications` | Active apps plus their column metadata (2 result sets) |
| `SP_CR_Get_Parameters` (@Model_ID) | Parameter list with a mapped count per app |
| `SP_CR_Save_Parameter` (@CR_Parameter_ID, @Name, …) | Create or rename, with a uniqueness check |
| `SP_CR_Delete_Parameter` | Soft delete the parameter and its active mappings |
| `SP_CR_Get_Parameter_Mappings` (@CR_Parameter_ID) | What is mapped (all apps) |
| `SP_CR_Save_Mappings` (@CR_Parameter_ID, @App_ID, @Source_Model_ID, TVP of leaf IDs and display text) | Bulk map in **one transaction**. Skips leaves already mapped and returns which ones were skipped and why |
| `SP_CR_Unmap` (@Mapping_ID list) | Soft unmap |
| `SP_CR_Get_Master_1D_TCF`, `_1D_BIW`, `_EST`, `_GD`, `_TORQUE`, `_CMM` (@Model_ID) | One per app, built from `query.sql`, all returning `Leaf_ID, Leaf_Name, …` |

**Cross-database access:** the per-app SPs read the source app databases using three-part names (`[DB].dbo.MM_LocationMaster`) or a linked server. **I need the DB names / server (Q3).** This is the only place an app's location is defined, so if a DB moves, only that one SP changes.

---

## 4. Backend (.NET 8): dedicated folder

The code follows existing patterns exactly: a controller derived from `BaseController`, a data service using `IDbConnectionFactory` with ADO.NET and SPs, `IApiResponseHelper` envelopes, and PascalCase/underscore DTOs.

```
API_PQ_Global_Reporting/
└── CorrelationModule/
    ├── Controllers/MM_CR_MappingController.cs
    ├── Data/MM_CR_MappingDataService.cs
    └── Models/MM_CR_MappingDtos.cs
```
The only edit to a shared file is **one line in `Program.cs`**: `AddScoped<MM_CR_MappingDataService>()`.

### Endpoints (`api/MM_CR_Mapping/...`)
| Method | Route | Body / Query |
|---|---|---|
| GET | `GetModels` | Plant from the user (see Q4) |
| GET | `GetApplications` | |
| GET | `GetParameters?modelId=` | |
| POST | `SaveParameter` | `{ CR_Parameter_ID?, Model_ID, Parameter_Name }` |
| POST | `DeleteParameter/{id}` | |
| GET | `GetParameterMappings/{id}` | |
| GET | `GetMasterData?appId=&crParameterId=` | Returns `{ Rows[] }`, each with `Is_Mapped` and `Other_Parameters` flags |
| POST | `SaveMappings` | `{ CR_Parameter_ID, App_ID, Source_Model_ID, Items:[{Leaf_ID, Display_Text}] }` |
| POST | `Unmap` | `{ Mapping_IDs: [] }` |

POST is used for updates and deletes to match the existing controllers. User and host are taken the same way the existing code does it.
**Safety:** `GetMasterData` takes only an `App_ID`. The SP name is read from `MM_CR_Application` and checked against `sys.procedures` before it runs, so there is no dynamic SQL from user input.

---

## 5. Frontend (Angular 18): dedicated folder

```
UI/src/app/Features/CorrelationModule/
├── correlation.routes.ts
├── correlation.model.ts
├── correlation.service.ts
├── mapping-master/          ← Screen 1 (slide 1)
└── parameter-mapping/       ← Screen 2 (slide 2)
```
Shared edits: **one route** in `app.routes.ts` (`/correlation`) plus a **menu row insert** in SQL (menus come from the DB).

### Screen 1 – Mapping Master (`/correlation/mapping-master`)
- **Model** dropdown at the top.
- A parameter table with columns *Sr.No | Parameter Name | Mapped (chips per app with counts, e.g. 1D BIW 3 · EST 1) | Action (Map · Edit · Delete)*.
- An **Add New Parameter** button opens an inline/modal input. The uniqueness check gives a friendly toast.
- Delete asks for confirmation through the existing `ConfirmationService`.

### Screen 2 – Parameter Mapping (`/correlation/mapping-master/:parameterId`)
- Header: Parameter Name and Model, plus a back button.
- **Tab bar of applications**, built from `GetApplications` (nothing hardcoded).
- **Available** table for the selected app: dynamic columns from metadata, column filters/search, row checkboxes and a **Map Selected** button.
  - Rows already mapped to this parameter are shown ticked/disabled.
  - Rows mapped to *another* parameter are shown disabled with a tooltip naming that parameter (if Q2 = yes).
- **Mapped to this parameter** panel: grouped by app, each item with an **Unmap** action (with confirmation).
- Styling uses Tailwind, matching the current Global Search look.

---

## 6. Deliverables and how you'll test on the other system

```
CorrelationModule/
├── PLAN.md                      ← this file
├── CHANGES.md                   ← every new/edited file listed (for copy-paste)
└── SQL/
    ├── 01_Tables.sql
    ├── 02_Seed_Applications.sql ← 6 apps + column metadata
    ├── 03_SPs_Mapping.sql
    ├── 04_SPs_Master_Data.sql   ← per-app SPs from query.sql
    └── 05_Menu_Insert.sql
```
All SQL scripts are re-runnable (`IF NOT EXISTS` / `CREATE OR ALTER`).
**Verification here:** `dotnet build` for the API and `ng build` for the UI must both pass. I can't run anything against SQL on this machine, so you will run the scripts and test on the other system.

**Order of work after approval:** SQL → API → UI → build checks → CHANGES.md.

---

## 7. Decisions (answered 2026-10-13)

| # | Decision |
|---|---|
| Q1 | Model_IDs differ per application, so matching is **by Model_Name**. `PQ_Dashboard_Master.dbo.MM_Model (Model_ID, Model_Name)` is the master list, and each app SP resolves its own Model_ID(s) from its own `MM_Model` by name |
| Q2 | One source leaf **may** be mapped to multiple global parameters. Only a duplicate of the same leaf to the same parameter is blocked |
| Q3 | Same server, different DBs: `[1D_Module]` (1D TCF/BIW), `[PQ_EST_GD]` (EST/GD), `[PQ_Torque_Audit]` (Torque). The per-app SPs live in `PQ_Dashboard_Master` and use 3-part names, so the API uses a single connection |
| Q4 | Models come from `MM_Model` in `PQ_Dashboard_Master` (added by you) |
| Q5 | CMM is not seeded. Add it later by creating its SP and inserting its rows (see `02_Seed_Applications.sql`) |
| Q6 | EST/GD are in a separate DB (`PQ_EST_GD`) |
| Q7 | Parameter names are unique **per model** |
| Q8 | Everyone with access to the Masters menu |

Because of Q1, the parameter has **no Plant_ID**; it is scoped by Model only.
