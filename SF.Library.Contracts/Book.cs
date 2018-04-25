using System;

namespace SF.Library.Contracts
{
    public class Book
    {
        public Guid Id { get; set; }

        public string Author { get; set; }

        public string Title { get; set; }

        public int Year { get; set; }
    }
}
