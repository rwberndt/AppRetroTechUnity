using System.Collections.Generic;

namespace RetroTech
{
    /// <summary>
    /// Represents a high level category of computer artifacts in the museum.  A category has a
    /// unique identifier, a human friendly name and a list of sub‑categories.  In the
    /// original Flutter implementation the category also tracked whether it was expanded in the
    /// user interface – the isExpanded flag allows us to mirror that behaviour in Unity when
    /// rendering the categories list.
    /// </summary>
    [System.Serializable]
    public class Category
    {
        public long Id;
        public string Name;
        public List<string> Subcategories;
        public bool IsExpanded;

        public Category(long id, string name, List<string> subcategories, bool isExpanded = false)
        {
            Id = id;
            Name = name;
            Subcategories = subcategories;
            IsExpanded = isExpanded;
        }

        public Category CopyWith(bool? isExpanded = null)
        {
            return new Category(Id, Name, Subcategories, isExpanded ?? IsExpanded);
        }
    }
}