# Database Schema Design

## Tables

### Books

- **Id** (int, PK, auto-increment)
- **ISBN** (string, unique, indexed)
- **Title** (string, required)
- **Publisher** (string)
- **PublicationYear** (int)
- **Description** (string)
- **CoverImageUrl** (string)
- **DateAdded** (DateTime)
- **Notes** (string)

### Authors

- **Id** (int, PK, auto-increment)
- **Name** (string)

### BookAuthors (Junction Table)

- **BookId** (int, FK -> Books.Id)
- **AuthorId** (int, FK -> Authors.Id)
- Primary Key: (BookId, AuthorId)

### Borrowers

- **Id** (int, PK, auto-increment)
- **Name** (string, required)
- **Email** (string, required only for email reminders)
- **Phone** (string)
- **DateAdded** (DateTime)

### Loans

- **Id** (int, PK, auto-increment)
- **BookId** (int, FK -> Books.Id)
- **BorrowerId** (int, FK -> Borrowers.Id)
- **CheckoutDate** (DateTime)
- **DueDate** (DateTime)
- **ReturnDate** (DateTime?)
- **Notes** (string)

### Renewals

- **Id** (int, PK, auto-increment)
- **LoanId** (int, FK -> Loans.Id)
- **RenewalDate** (DateTime)
- **Notes** (string)

### Categories

- **Id** (int, PK, auto-increment)
- **Name** (string, unique)

### BookCategories (Junction Table)

- **BookId** (int, FK -> Books.Id)
- **CategoryId** (int, FK -> Categories.Id)
- Primary Key: (BookId, CategoryId)

### Tags

- **Id** (int, PK, auto-increment)
- **Name** (string, unique)

### BookTags (Junction Table)

- **BookId** (int, FK -> Books.Id)
- **TagId** (int, FK -> Tags.Id)
- Primary Key: (BookId, TagId)

### Collections

- **Id** (int, PK, auto-increment)
- **Name** (string)
- **Notes** (string)

### BookCollections (Junction Table)

- **BookId** (int, FK -> Books.Id)
- **CollectionId** (int, FK -> Collections.Id)
- Primary Key: (BookId, CollectionId)

## Relationships

- Book -> Loans (1-to-many)
- Borrower -> Loans (1-to-many)
- Book <-> Categories (many-to-many via BookCategories)
- Loan -> Renewals (1-to-many)
- Book <-> Authors (many-to-many via BookAuthors)
- Book <-> Tags (many-to-many via BookTags)
- Book <-> Collections (many-to-many via BookCollections)

## Indexes

- Books.ISBN (unique)
- Books.Title (for search)
- Authors.Name (for search)
- Loans.BookId
- Loans.BorrowerId
