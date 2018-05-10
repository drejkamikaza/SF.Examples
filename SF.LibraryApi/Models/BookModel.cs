using System;

namespace SF.LibraryApi.Models
{
    public class BookModel
    {
        public Guid Id { get; set; }

        public string Author { get; set; }

        public string Title { get; set; }

        public int Year { get; set; }
    }
}
