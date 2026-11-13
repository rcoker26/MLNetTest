# Walworth County Residential Sale Price Prediction — ML.NET

**Project folder:** `C:\Projects\MLNetTest`  
**Document updated:** November 13, 2026  
**Development environment:** Visual Studio 2022, C#, ML.NET Model Builder  
**Task:** Value prediction (regression) of residential sale price in Walworth County, Wisconsin

## 1. Objective

Build and evaluate a local home-sale-price prediction model using actual Walworth County property sales and Wisconsin parcel assessment information. The prediction target is `SalePrice` (US dollars).

## 2. Data sources and preparation

1. **Real estate transfers:** Wisconsin real estate transfer records (`202611RETRReport.csv`), filtered to relevant residential sales. The intermediate file is `WalworthResidentialSales.csv`.
2. **Parcel data:** Wisconsin Statewide Parcel Database, Version 12, Walworth County export. The county parcel geodatabase was opened in QGIS and exported as `WalworthParcels.csv`. County data access: <https://www.sco.wisc.edu/parcels/data-county/#v1200>.
3. **Merge:** Sales were matched to parcel records to add assessment and lot acreage fields. Intermediate outputs: `WalworthSalesParcelsMerged.csv` and `WalworthParcelMatchReview.csv` for records needing review.
4. **Limitation:** The available exports did not provide reliable bedrooms, bathrooms, year built, or finished living area for this training exercise.

Parcel and sale identifiers should be kept as text when opening CSV files in Excel to preserve leading zeros.

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

1. **Save this file** as `C:\Projects\MLNetTest\MLNetHomePriceModel.md`.
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
