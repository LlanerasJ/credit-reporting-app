# Synthetic Metro 2-Style Field Map

This project generates and parses files that follow the **publicly known structure**
of the Metro 2® credit-reporting format: a header record, fixed-width 426-character
base segments (one per account) with optional appended segments, and a trailer with
control totals.

The exact field positions below are a **synthetic, documented approximation created
for this demo**. They are *not* the CDIA's licensed Credit Reporting Resource Guide
field tables, which are proprietary. Do not use these files for actual furnishing.

All positions are 1-based and inclusive. Types: **A** = alpha (left-justified,
space-padded), **N** = numeric (right-justified, zero-padded, whole dollars),
**D** = date `yyyyMMdd` (eight zeros when empty).

## Header record (426 chars, one per file)

| Pos | Len | Type | Field |
|----:|----:|:----:|-------|
| 1 | 4 | A | Record descriptor word (`0426`) |
| 5 | 6 | A | Record identifier (`HEADER`) |
| 11 | 2 | A | Cycle identifier |
| 13 | 10 | A | Program identifier (furnisher ID) |
| 23 | 8 | D | Activity date |
| 31 | 8 | D | Date created |
| 39 | 8 | D | Program date |
| 47 | 8 | D | Program revision date |
| 55 | 40 | A | Reporter name |
| 95 | 96 | A | Reporter address |
| 191 | 10 | A | Reporter telephone |
| 201 | 40 | A | Software vendor name |
| 241 | 5 | A | Software version |
| 246 | 181 | A | Reserved |

## Base segment (426 chars, one per account)

| Pos | Len | Type | Field |
|----:|----:|:----:|-------|
| 1 | 4 | A | Record descriptor word (`0426`) |
| 5 | 1 | A | Processing indicator (`1`) |
| 6 | 14 | A | Timestamp (`yyyyMMddHHmmss`) |
| 20 | 1 | A | Correction indicator |
| 21 | 20 | A | Identification number (furnisher) |
| 41 | 2 | A | Cycle identifier |
| 43 | 30 | A | Consumer account number |
| 73 | 1 | A | Portfolio type (C/I/M/O/R) |
| 74 | 2 | A | Account type code |
| 76 | 8 | D | Date opened |
| 84 | 9 | N | Credit limit |
| 93 | 9 | N | Highest credit |
| 102 | 3 | A | Terms duration |
| 105 | 1 | A | Terms frequency |
| 106 | 9 | N | Scheduled monthly payment |
| 115 | 9 | N | Actual payment |
| 124 | 2 | A | Account status code |
| 126 | 1 | A | Payment rating (0–6, G, L) |
| 127 | 24 | A | Payment history profile (24 months, newest first) |
| 151 | 2 | A | Special comment |
| 153 | 2 | A | Compliance condition code |
| 155 | 9 | N | Current balance |
| 164 | 9 | N | Amount past due |
| 173 | 9 | N | Original charge-off amount |
| 182 | 8 | D | Date of account information |
| 190 | 8 | D | Date of first delinquency |
| 198 | 8 | D | Date closed |
| 206 | 8 | D | Date of last payment |
| 214 | 1 | A | Interest type indicator |
| 215 | 16 | A | Reserved |
| 231 | 25 | A | Surname |
| 256 | 20 | A | First name |
| 276 | 20 | A | Middle name |
| 296 | 1 | A | Generation code |
| 297 | 9 | A | Social Security number (synthetic 900-range only) |
| 306 | 8 | D | Date of birth |
| 314 | 10 | A | Telephone number |
| 324 | 1 | A | ECOA code |
| 325 | 2 | A | Consumer information indicator |
| 327 | 2 | A | Country code |
| 329 | 32 | A | Address line 1 |
| 361 | 32 | A | Address line 2 |
| 393 | 20 | A | City |
| 413 | 2 | A | State |
| 415 | 9 | A | Postal code |
| 424 | 1 | A | Address indicator |
| 425 | 1 | A | Residence code |
| 426 | 1 | A | Reserved |

## Appended segments (optional, same line, after position 426)

Segments may appear in any order; each is identified by its first two characters.

### J1: associated consumer, same address (100 chars)

| Pos | Len | Type | Field |
|----:|----:|:----:|-------|
| 1 | 2 | A | Segment identifier (`J1`) |
| 3 | 25 | A | Surname |
| 28 | 20 | A | First name |
| 48 | 20 | A | Middle name |
| 68 | 1 | A | Generation code |
| 69 | 9 | A | Social Security number |
| 78 | 8 | D | Date of birth |
| 86 | 10 | A | Telephone number |
| 96 | 1 | A | ECOA code |
| 97 | 2 | A | Consumer information indicator |
| 99 | 2 | A | Reserved |

### K1: original creditor (34 chars)

| Pos | Len | Type | Field |
|----:|----:|:----:|-------|
| 1 | 2 | A | Segment identifier (`K1`) |
| 3 | 30 | A | Original creditor name |
| 33 | 2 | A | Creditor classification |

## Trailer record (426 chars, one per file)

| Pos | Len | Type | Field |
|----:|----:|:----:|-------|
| 1 | 4 | A | Record descriptor word (`0426`) |
| 5 | 7 | A | Record identifier (`TRAILER`) |
| 12 | 9 | N | Total base records |
| 21 | 9 | N | Total J1 segments |
| 30 | 9 | N | Total K1 segments |
| 39 | 9 | N | Total SSNs reported |
| 48 | 9 | N | Total dates of birth reported |
| 57 | 9 | N | Total telephone numbers reported |
| 66 | 361 | A | Reserved |

## Code sets used by the demo

- **Portfolio type:** `C` line of credit, `I` installment, `M` mortgage, `O` open, `R` revolving
- **Account type:** `00` auto, `01` unsecured/personal, `07` retail card, `12` education, `15` line of credit, `18` credit card, `26` mortgage
- **Account status:** `11` current, `13` paid/closed, `61`–`65` closed variants, `71`/`78`/`80`/`82`/`83`/`84` 30–180+ days past due, `93` collection, `97` charge-off
- **Payment rating / history profile:** `0` current, `1`–`6` = 30/60/90/120/150/180+ days late, `B` no history available, plus `D E G H J K L` accepted on parse
- **ECOA:** `1` individual, `2` joint, `3` authorized user, `5` co-maker, `7` maker, `T` terminated, `W` business, `X` deceased, `Z` delete

The validator (`Metro2Validator`) enforces required fields, these code sets,
24-character history profiles, and date sanity (no future open dates, closed ≥ opened),
reporting **errors** (block generation) separately from **warnings** (informational).
