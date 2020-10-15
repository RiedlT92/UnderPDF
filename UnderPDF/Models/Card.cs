using System;

namespace UnderPDF
{
    public class Card
    {
        public string Release { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string Faction { get; set; }
        public string Type { get; set; }
        public string ObjType { get; set; }
        public string Set { get; set; }
        public string OrganisedPlay { get; set; }
        public string Forsaken { get; set; }
        public string Restricted { get; set; }

        public static Card FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(';');
            Card card = new Card();
            card.Release = Convert.ToString(values[0]);
            card.Number = Convert.ToString(values[2]);
            card.Name = Convert.ToString(values[3]);
            card.Faction = Convert.ToString(values[4]);
            card.Type = Convert.ToString(values[5]);
            card.ObjType = Convert.ToString(values[13]);
            card.Set = Convert.ToString(values[15]);
            card.OrganisedPlay = Convert.ToString(values[17]);
            card.Forsaken = Convert.ToString(values[19]);
            card.Restricted = Convert.ToString(values[20]);

            return card;
        }
    } 
}
