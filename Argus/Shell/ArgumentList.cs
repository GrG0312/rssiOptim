using System.Collections;

namespace VisiLib.Args.Shell
{
    /// <summary>
    /// Represents a list of command-line arguments.
    /// It is a wrapper around <see cref="IReadOnlyList{T}" /> - where T is a <see cref="string"/> - that provides additional methods for argument handling.
    /// </summary>
    ///
    /// <remarks>
    /// This class is used instead of a simple string array to provide a more convenient and expressive way to work with command-line arguments.
    /// </remarks>
    public sealed class ArgumentList : IReadOnlyList<string>
    {
        /// <summary>
        /// The backing list of argument strings.
        /// </summary>
        private readonly IReadOnlyList<string> _items;

        /// <summary>
        /// An empty argument list.
        /// </summary>
        public static ArgumentList Empty { get; } = new ArgumentList([]);

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentList"/> class with the specified items.
        /// </summary>
        ///
        /// <param name="items">
        /// The list of argument strings to wrap.
        /// </param>
        public ArgumentList(IReadOnlyList<string> items)
        {
            _items = items;
        }

        /// <inheritdoc />
        public int Count => _items.Count;

        /// <inheritdoc />
        public string this[int index] => _items[index];

        /// <summary>
        /// Indicates whether the argument list is empty.
        /// </summary>
        public bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// Returns the argument at the specified index, or null if the index is out of range.
        /// </summary>
        public string? At(int index)
        {
            if (index < _items.Count)
            {
                return _items[index];
            }
            return null;
        }

        /// <summary>
        /// Returns the argument at the specified index, or throws a <see cref="VisiArgException"/> if the index is out of range.
        /// </summary>
        ///
        /// <param name="index">
        /// The index of the argument to retrieve.
        /// </param>
        ///
        /// <param name="name">
        /// The name of the argument, used in the exception message if the argument is missing.
        /// </param>
        ///
        /// <param name="usage">
        /// Optional usage information to include in the exception message if the argument is missing.
        /// </param>
        ///
        /// <exception cref="VisiArgException"></exception>
        public string Require(int index, string name, string? usage = null)
        {
            string? value = At(index);
            if (value is null)
            {
                throw new VisiArgException($"Missing: {name}.", usage);
            }
            return value;
        }

        /// <summary>
        /// Returns a string that concatenates all arguments starting from <paramref name="index"/>, separated by spaces.
        /// </summary>
        ///
        /// <returns>
        /// A string containing all arguments from the specified index, or an empty string if the index is out of range.
        /// </returns>
        public string From(int index)
        {
            if (index < _items.Count)
            {
                return string.Join(' ', _items.Skip(index));
            }
            return string.Empty;
        }

        /// <inheritdoc />
        public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
