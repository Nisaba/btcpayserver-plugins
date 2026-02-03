using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Shopstr.Models
{
    [PrimaryKey(nameof(StoreId), nameof(AppId))]
    public class ShopstrSettings
    {
        [Key]
        public string StoreId { get; set; }
        [Key]
        public string AppId { get; set; }

        public string Location { get; set; }

        public bool FlashSales { get; set; }

        public ConditionEnum Condition { get; set; }

        public DateTimeOffset? ValidDateT { get; set; }

        public string Restrictions { get; set; }

        public static readonly Dictionary<string, string[]> AvailableLocations = new()
        {
            ["Regional"] =
            [
                "Worldwide", "US & Canada", "Europe", "Online"
            ],
            ["Countries"] =
            [
                "Afghanistan", "Albania", "Algeria", "American Samoa", "Andorra", "Angola", "Anguilla", "Antigua & Barbuda", "Argentina", "Armenia",
                "Aruba", "Australia", "Austria", "Azerbaijan", "Bahamas", "Bahrain", "Bangladesh", "Barbados", "Belarus", "Belgium", "Belize", "Benin",
                "Bermuda", "Bhutan", "Bolivia", "Bonaire", "Bosnia & Herzegovina", "Botswana", "Brazil", "British Indian Ocean Ter", "Brunei",
                "Bulgaria", "Burkina Faso", "Burundi", "Cambodia", "Cameroon", "Canada", "Cape Verde", "Cayman Islands", "Central African Republic",
                "Chad", "Channel Islands", "Chile", "China", "Christmas Island", "Cocos Island", "Colombia", "Comoros", "Congo", "Cook Islands",
                "Costa Rica", "Côte d’Ivoire", "Croatia", "Cuba", "Curaçao", "Cyprus", "Czech Republic", "Denmark", "Djibouti", "Dominica",
                "Dominican Republic", "East Timor", "Ecuador", "Egypt", "El Salvador", "Equatorial Guinea", "Eritrea", "Estonia", "Ethiopia",
                "Falkland Islands", "Faroe Islands", "Fiji", "Finland", "France", "French Guiana", "French Polynesia", "French Southern Ter",
                "Gabon", "Gambia", "Georgia", "Germany", "Ghana", "Gibraltar", "Great Britain", "Greece", "Greenland", "Grenada", "Guadeloupe",
                "Guam", "Guatemala", "Guinea", "Guyana", "Haiti", "Honduras", "Hong Kong", "Hungary", "Iceland", "India", "Indonesia", "Iran",
                "Iraq", "Ireland", "Isle of Man", "Israel", "Italy", "Jamaica", "Japan", "Jordan", "Kazakhstan", "Kenya", "Kiribati", "Korea North",
                "Korea South", "Kuwait", "Kyrgyzstan", "Laos", "Latvia", "Lebanon", "Lesotho", "Liberia", "Libya", "Liechtenstein", "Lithuania",
                "Luxembourg", "Macau", "Macedonia", "Madagascar", "Malaysia", "Malawi", "Maldives", "Mali", "Malta", "Marshall Islands",
                "Martinique", "Mauritania", "Mauritius", "Mayotte", "Mexico", "Moldova", "Monaco", "Mongolia", "Montserrat", "Morocco",
                "Mozambique", "Myanmar", "Namibia", "Nauru", "Nepal", "Netherlands (Holland, Europe)", "New Caledonia", "New Zealand", "Nicaragua",
                "Niger", "Nigeria", "Niue", "Norfolk Island", "Norway", "Oman", "Pakistan", "Palau Island", "Palestine", "Panama",
                "Papua New Guinea", "Paraguay", "Peru", "Philippines", "Pitcairn Island", "Poland", "Portugal", "Puerto Rico", "Qatar",
                "Republic of Montenegro", "Republic of Serbia", "Reunion", "Romania", "Russia", "Rwanda", "St Barthelemy", "St Eustatius",
                "St Helena", "St Kitts-Nevis", "St Lucia", "St Maarten", "St Pierre & Miquelon", "St Vincent & Grenadines", "Saipan", "Samoa",
                "Samoa American", "San Marino", "Sao Tome & Principe", "Saudi Arabia", "Senegal", "Seychelles", "Sierra Leone", "Singapore",
                "Slovakia", "Slovenia", "Solomon Islands", "Somalia", "South Africa", "Spain", "Sri Lanka", "Sudan", "Suriname", "Swaziland",
                "Sweden", "Switzerland", "Syria", "Tahiti", "Taiwan", "Tajikistan", "Tanzania", "Thailand", "Togo", "Tokelau", "Tonga",
                "Trinidad & Tobago", "Tunisia", "Turkey", "Turkmenistan", "Turks & Caicos Is", "Tuvalu", "Uganda", "Ukraine", "United Arab Emirates",
                "United Kingdom", "United States of America", "Uruguay", "Uzbekistan", "Vanuatu", "Vatican City State", "Venezuela", "Vietnam",
                "Virgin Islands (Brit)", "Virgin Islands (USA)", "Wake Island", "Wallis & Futuna Is", "Yemen", "Zambia", "Zimbabwe"
            ],
            ["U.S. states"] =
            [
                "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado", "Connecticut", "Delaware", "Florida", "Hawaii", "Idaho",
                "Illinois", "Indiana", "Iowa", "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland", "Massachusetts", "Michigan", "Minnesota",
                "Mississippi", "Missouri", "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey", "New Mexico", "New York",
                "North Carolina", "North Dakota", "Ohio", "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina",
                "South Dakota", "Tennessee", "Texas", "Utah", "Vermont", "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming"
            ]
        };
    }

    public enum ConditionEnum
    {
        [Description("")]
        None,

        [Description("New")]
        New,

        [Description("Renewed")]
        Renewed,

        [Description("Used - Like New")]
        UsedLikeNew,

        [Description("Used - Very Good")]
        UsedVeryGood,

        [Description("Used - Good")]
        UsedGood,

        [Description("Used - Acceptable")]
        UsedAcceptable
    }

}
