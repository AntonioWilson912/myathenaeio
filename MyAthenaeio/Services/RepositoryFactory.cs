using MyAthenaeio.Data;
using MyAthenaeio.Data.Repositories;

namespace MyAthenaeio.Services
{
    public class RepositoryFactory : IDisposable
    {
        private readonly AppDbContext _context;
        private BookRepository? _bookRepositoory;
        private BookCopyRepository? _bookCopyRepository;
        private AuthorRepository? _authorRepository;
        private LoanRepository? _loanRepository;
        private BorrowerRepository? _borrowerRepository;
        private GenreRepository? _genreRepository;
        private TagRepository? _tagRepository;
        private CollectionRepository? _collectionRepository;

        public RepositoryFactory()
        {
            _context = new AppDbContext();
        }

        public RepositoryFactory(AppDbContext context)
        {
            _context = context;
        }

        public IBookRepository Books => _bookRepositoory ??= new BookRepository(_context);
        public IBookCopyRepository BookCopies => _bookCopyRepository ??= new BookCopyRepository(_context);
        public IAuthorRepository Authors => _authorRepository ??= new AuthorRepository(_context);
        public ILoanRepository Loans => _loanRepository ??= new LoanRepository(_context);
        public IBorrowerRepository Borrowers => _borrowerRepository ??= new BorrowerRepository(_context);
        public IGenreRepository Genres => _genreRepository ??= new GenreRepository(_context);
        public ITagRepository Tags => _tagRepository ??= new TagRepository(_context);
        public ICollectionRepository Collections => _collectionRepository ??= new CollectionRepository(_context);

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
