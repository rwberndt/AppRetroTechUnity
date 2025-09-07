using System.Collections.Generic;

namespace RetroTech
{
    /// <summary>
    /// Represents a single artefact in the museum.  Each ComputerPiece encapsulates
    /// metadata used throughout the app including an identifier, category, year of
    /// manufacture and descriptive fields.  Specifications are kept as a list of
    /// strings so they can be rendered flexibly by the UI.
    /// </summary>
    [System.Serializable]
    public class ComputerPiece
    {
        public string Id;
        public string Name;
        public string Category;
        public int YearManufactured;
        public string Manufacturer;
        public string Description;
        public string ImageUrl;
        public string Curiosities;
        public List<string> Specifications;

        public ComputerPiece(
            string id,
            string name,
            string category,
            int yearManufactured,
            string manufacturer,
            string description,
            string imageUrl,
            string curiosities,
            List<string> specifications)
        {
            Id = id;
            Name = name;
            Category = category;
            YearManufactured = yearManufactured;
            Manufacturer = manufacturer;
            Description = description;
            ImageUrl = imageUrl;
            Curiosities = curiosities;
            Specifications = specifications;
        }
    }
}