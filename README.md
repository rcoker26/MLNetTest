# Walworth County Residential Sale Price Prediction — ML.NET

**Project folder:** `C:\Projects\MLNetTest`  
**Document updated:** November 13, 2026  
**Development environment:** Visual Studio 2022, C#, ML.NET Model Builder  
**Task:** Value prediction (regression) of residential sale price in Walworth County, Wisconsin

## 1. Objective

Build and evaluate a local home-sale-price prediction model using actual Walworth County property sales and Wisconsin parcel assessment information. The prediction target is `SalePrice` (US dollars).

## 2. Data sources and preparation

This project combines **transaction data** (what a property sold for) with **geospatial parcel/assessment data** (which parcel it is and what the land and improvements were assessed at). The GIS processing step was essential because the real estate transfer export did not provide all the property-level predictors needed for modeling.

### 2.1 Wisconsin real estate transfer records

The source sales export was `202611RETRReport.csv`. Residential transactions were filtered into `WalworthResidentialSales.csv`, using transaction and property-use fields to focus on relevant sales. The sales dataset supplies the regression label, `SalePrice`, and the transaction-side identifiers used to associate sales with parcels.

The filtering process was intended to exclude transfers that are not comparable arm's-length residential sales. Filtering criteria and any exclusions should be checked against the original source columns before reproducing the workflow.

### 2.2 Wisconsin Statewide Parcel Database (Version 12)

The parcel source was the **Wisconsin Statewide Parcel Database, Version 12**, obtained through the State Cartographer's Office county download portal:

<https://www.sco.wisc.edu/parcels/data-county/#v1200>

The **Walworth County** dataset was downloaded as a GIS geodatabase rather than relying on a single countywide assessor spreadsheet. The geodatabase contains mapped parcel features and associated tabular attributes. A parcel feature is a geographic representation of a land parcel; its attribute table holds identifiers and property/assessment details.

For this project, the useful exported fields included parcel/tax identifiers, site address, property classification, assessed land value, assessed improvement value, and parcel acreage. The available export **did not reliably provide** bedrooms, bathrooms, year built, or finished living area.

### 2.3 QGIS: geodatabase inspection and export

**Tool:** QGIS, a desktop geographic information system (GIS) used to inspect spatial datasets, map parcel boundaries, and work with their attribute tables.

The project workflow was:

1. **Acquire the county GIS dataset.** Download the Walworth County Version 12 parcel data and extract the downloaded archive as needed. Retain the source geodatabase unchanged as a reproducible input.
2. **Load the geodatabase into QGIS.** Use QGIS's data-source/browser controls to locate the file geodatabase (`.gdb`) and add its parcel feature layer to the project. A geodatabase may contain multiple layers; select the parcel layer rather than assuming every layer is a parcel table.
3. **Inspect the map.** Display Walworth County parcel geometry on the map canvas. This is useful for verifying that the selected layer represents the intended county and that features are present across the study area.
4. **Inspect the attribute table.** Open the parcel layer's attribute table to identify parcel keys, site addresses, property class, assessed values, and acreage. Check field types and null/blank values before exporting.
5. **Identify the fields needed for ML.NET.** The training model does not use parcel geometry directly; it uses selected *attributes* derived from the GIS dataset. Preserve parcel identifiers as text and assessment/acreage fields as numeric values.
6. **Export the parcel attributes to CSV.** In QGIS, right-click the parcel layer and choose **Export → Save Features As…**. Select **Comma Separated Value (CSV)**, choose the destination, and save as `WalworthParcels.csv`. CSV is a flat tabular format; it does not preserve the interactive parcel geometry or QGIS layer styling.
7. **Review the exported CSV.** Confirm row counts, field names, decimal values, and identifier formatting. Opening CSVs in Excel can strip leading zeros from parcel IDs; import those fields as **Text** or choose **Don't Convert** when prompted.

**Local working location:** `C:\Projects\MLNetTest\data\` was used for the project's CSV inputs. QGIS project files and original geodatabase downloads can be kept separately from derived training tables.

#### GIS concepts applied

| GIS concept | How it was used in this project |
|---|---|
| **Parcel geometry** | Visual confirmation of mapped land parcels within Walworth County |
| **Feature layer** | QGIS representation of the county parcel dataset |
| **Attribute table** | Source of parcel IDs, property class, assessment fields, and acreage |
| **Geodatabase (`.gdb`)** | Original spatial-data container opened in QGIS |
| **CSV export** | Conversion of GIS attributes into a format suitable for tabular ETL and ML.NET |
| **Parcel identifier** | Candidate key for linking transactions to parcel attributes |
| **Site address** | Supporting reference for investigating possible parcel matches |
| **Coordinate reference system (CRS)** | Relevant to spatial geometry; no coordinate reprojection or spatial-distance feature engineering is claimed for this model |

This workflow is **GIS-assisted tabular feature engineering**, not a spatial machine-learning model. Parcel shapes were inspected in QGIS, but no geometry, coordinates, proximity-to-lake measurements, or other spatial features were included in the final training CSV.

### 2.4 Joining sales records to parcel attributes

The sales and parcel files represent different data sources and must be linked carefully. The processing pipeline associated residential transactions with parcel records and produced:

- `WalworthSalesParcelsMerged.csv` — combined sales and parcel assessment fields.
- `WalworthParcelMatchReview.csv` — cases needing manual review because a match was missing or ambiguous.

The general ETL sequence was:

1. Read the filtered sales and QGIS parcel CSVs.
2. Normalize candidate parcel identifiers consistently while **preserving meaningful leading zeros**. Avoid numeric conversion of identifiers.
3. Match records using the appropriate parcel/tax identifier fields; review unmatched or non-unique matches rather than assigning arbitrary parcels.
4. Carry across relevant attributes such as `AssessedLandValue`, `AssessedImprovementValue`, and `LotAcres`.
5. Validate resulting values and retain only usable records for the modeling experiments.

**Reproducibility note:** The exact join-field mapping, normalization rules, and exception-resolution logic are not included in this README. They should be documented from the original merge script or working notes before claiming a fully reproducible automated join. A street address alone should not be treated as a guaranteed unique parcel key.

### 2.5 Building the ML.NET feature table

The GIS-derived assessment and acreage columns were combined with sale-side categorical attributes and the `SalePrice` target. The original model used five predictors. The revised model added `TotalAssessedValue`, calculated as:

```text
TotalAssessedValue = AssessedLandValue + AssessedImprovementValue
```

The revised dataset excludes transactions with sale prices of $1 million or more. That restriction changes the population being modeled; it does not mean the resulting model generalizes to luxury or lakefront homes.

**Data lineage**

```text
Wisconsin real estate transfers ──> Residential sales filtering ─────┐
                                                                      │
Wisconsin V12 Walworth parcels (.gdb)                                  │
    └─> QGIS map / attribute inspection                                │
          └─> WalworthParcels.csv ──> Parcel ID matching / QA ─────────┤
                                                                      v
                                               Merged sales + parcel attributes
                                                                      │
                                                                      v
                                                  ML.NET training CSVs
                                                                      │
                                                                      v
                                              Model Builder regression model
```

### 2.6 GIS data-quality considerations

- **Identifier integrity:** Parcel IDs are identifiers, not numbers. Preserve leading zeros and any formatting that matters to matching.
- **One-to-many relationships:** A sale can involve multiple parcels, and parcel identifiers can change over time. Flag ambiguous joins for review.
- **Assessment timing:** Confirm that assessment values were available *before* the transaction being predicted. Post-sale assessments may introduce information leakage.
- **Area semantics:** GIS parcel acreage describes land area, not building living area. It must not be interpreted as square footage of the home.
- **Missing building characteristics:** Bedroom count, bathroom count, construction year, and finished living area were not available as reliable training inputs in this export.
- **Spatial bias:** Lakefront location, lake access, neighborhood, and other geographic effects can influence price, but were not explicitly modeled using GIS-derived spatial features.
- **Public repository hygiene:** Review licensing, redistribution terms, and personally identifying transaction information before committing source datasets or parcel-level exports to a public GitHub repository.

### 2.7 Potential future GIS enhancements

A later version could add geospatial predictors by working in QGIS or a spatial database: parcel centroid coordinates, distance to Geneva Lake, lakefront adjacency, distance to amenities, or municipality/neighborhood boundaries. Such features require a documented CRS, valid geometries, appropriate distance calculations, and independent evaluation; **they were not implemented in the current model**.

## 3. Original ML.NET experiment

**Training file:** `WalworthMLNetTraining.csv`  
**Rows:** 218  
**Target:** `SalePrice`  
**Input features:**

- `Municipality` — categorical/text
- `PropertyType` — categorical/text
- `LotAcres` — numeric
- `AssessedLandValue` — numeric
- `AssessedImprovementValue` — numeric

**Model Builder task:** Value prediction (Regression)  
**Reported Evaluate-screen R-squared:** **0.0014**

This is a weak result on the evaluation data. The original file included high-end transactions up to **$8,880,000**, which may have made the modeling problem harder. That alone does not prove why the score was low.

## 4. Revised ML.NET experiment

**Training file:** `WalworthMLNetTraining_Revised.csv`  
**Rows:** 188  
**Target:** `SalePrice`  
**Revision:** Removed 30 sales at or above $1,000,000; retained sales below $1,000,000 and added `TotalAssessedValue`.

| Column | Role | Type |
|---|---|---|
| `Municipality` | Feature | Text/categorical |
| `PropertyType` | Feature | Text/categorical |
| `LotAcres` | Feature | Numeric |
| `AssessedLandValue` | Feature | Numeric |
| `AssessedImprovementValue` | Feature | Numeric |
| `TotalAssessedValue` | Feature | Numeric |
| `SalePrice` | Label/target | Numeric |

`TotalAssessedValue = AssessedLandValue + AssessedImprovementValue`; it is a derived feature, not an independent source of information.

**Reported Model Builder Evaluate-screen R-squared:** **0.9720**.

This indicates a strong fit on Model Builder's evaluation data, but **does not mean individual predictions are within 2.8% of sale price**. The first and second R-squared scores are **not directly comparable**, because the evaluated populations differ.

### Dataset checks

The revised CSV contains 188 rows, seven columns, and no blank values in those seven columns. The observed sale-price range is **$78,500 to $990,000**. The derived total assessed value equals the sum of its two components for all rows.

## 5. Visual Studio 2022 workflow

1. Open the `MLNetTest` solution in Visual Studio 2022.
2. Open the Model Builder `.mbconfig` file (or add Machine Learning through the project context menu if setting up again).
3. Choose **Value prediction (Regression)**.
4. Select `C:\Projects\MLNetTest\data\WalworthMLNetTraining_Revised.csv` (or its actual saved location).
5. Set **Column to predict** to `SalePrice` and select the other six columns as input features.
6. Train locally, then inspect **Evaluate**. The observed revised result was **R² = 0.9720**.
7. Proceed to **Consume** / generated code when ready; use **Add to solution** if Model Builder offers it. Generated file names vary by version and project configuration.

## 6. Interpretation and caveats

- An R² of 0.9720 means approximately 97.2% of the variation in the *evaluated sample's* sale prices is explained by the model, subject to the evaluation method used.
- The exact Model Builder split, random seed, trainer, and other training parameters have **not been documented**; retrieve them from `.mbconfig` or generated artifacts if reproducibility is needed.
- Assessment values can be closely related to sale prices. Confirm assessment dates precede the predicted sale dates to avoid time-related leakage.
- Restrict the current model's intended use to homes comparable to the revised training population (sales below $1 million). It has not been validated for luxury/lakefront transactions.
- With only 188 rows, results may vary substantially across train/test splits. Do not treat the reported score as independent validation.
- `TotalAssessedValue` is redundant with the land and improvement components; compare model performance with and without it.

## 7. Next steps

1. **Save this file** as `C:\Projects\MLNetTest\README.md` so GitHub renders it on the repository homepage.
2. Save both training CSVs in the project's `data` folder and keep the original unchanged.
3. Generate the ML.NET C# consumption code and model artifact from Model Builder.
4. Evaluate on a genuinely untouched set of later home sales, ensuring no overlap with training and no post-sale assessment information.
5. Calculate **MAE** (mean absolute error, in dollars), **RMSE**, and R² on that independent test set; compare against a baseline such as the median sale price or an assessed-value-based estimate.
6. Inspect actual-versus-predicted prices and error by municipality and price band.
7. If greater coverage is needed, obtain living area, bedrooms, bathrooms, and year built from suitable assessor or listing data, and retrain.

## 8. Project data files

| File | Purpose |
|---|---|
| `202611RETRReport.csv` | Original real estate transfer export |
| `WalworthResidentialSales.csv` | Filtered residential sales |
| `WalworthParcels.csv` | Walworth County parcel export from QGIS |
| `WalworthSalesParcelsMerged.csv` | Sales joined to parcel information |
| `WalworthParcelMatchReview.csv` | Parcel matching exceptions for review |
| `WalworthMLNetTraining.csv` | Original 218-row training dataset |
| `WalworthMLNetTraining_Revised.csv` | Revised 188-row training dataset |

**Status as of November 13, 2026:** Revised model trained and evaluated in Model Builder; user confirmed R² **0.9720** on the **Evaluate** screen. Generated C# consumption code and independent test-set results are not yet confirmed.
