# ✨ Features

- **Students & Courses**: CRUD screens with validation and anti-forgery protection  
- **Enrollments**: Students can enroll in courses. Enrolling automatically posts accounting entries  
- **Payments**: Create payments and (optionally) edit/delete with accounting consistency rules  
- **Double-Entry Accounting**: Ledger maintains debits/credits across Accounts (AR, CASH, REV)  
- **Global Balance Report (RDLC)**: One-click dashboard button generates a per-student balance statement  
- **EF6 + MySQL (InnoDB)**: Transactions, foreign keys, and cascading as appropriate  

---

# 🏗️ Architecture

<img width="846" height="169" alt="image" src="https://github.com/user-attachments/assets/c95979bb-6882-44e7-a32f-bfb87f4505dc" />

## Domain Model (core tables)

- **Students** (`StudentId`, `Name`, `Age`, …)  
- **Courses** (`CourseId`, `Title`, `Cost`, …)  
- **CourseSelections** (`CourseSelectionId`, `StudentId`, `CourseId`, `SelectionDate`) — enrollment  
- **Payments** (`PaymentId`, `StudentId`, `Amount`, `PaidAt`, `Reference`)  
- **Accounts** (`Id`, `Code [AR|CASH|REV]`, `Name`)  
- **LedgerEntries** (`Id`, `StudentId?`, `CourseSelectionId?`, `PaymentId?`, `AccountId`, `Debit`, `Credit`, `PostedAt`)  

## Double-Entry Rules

- On **enrollment**: **DR AR** (Accounts Receivable), **CR REV** (Revenue) by `Course.Cost`  
- On **payment**: **DR CASH**, **CR AR** by `Payment.Amount`  

**Student Balance Formula**:  
- Σ Charges (AR Debits) − Σ Payments (AR Credits)

# 🧰 Prerequisites
## 1. Connection string
Set **Web.config** `connectionStrings` entry named **SchoolContext** 
Use your **own connection string** 

## 2. Seed the Accounting table after migration 
INSERT INTO Accounts (Code, Name) VALUES
('AR',   'Accounts Receivable'),
('CASH', 'Cash'),
('REV',  'Revenue');



