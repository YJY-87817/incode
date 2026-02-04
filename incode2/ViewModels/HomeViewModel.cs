using incode2.Models;

namespace incode2.ViewModels
{
    public class HomeViewModel
    {
        public List<int>? displaycounts { get; set; }
        
        public List<HomeUser>? userdisplay { get; set; }

    }

    public class HomeUser 
    {
        public string createyear { get; set; }

        public string createmonth { get; set; }

        public int usercount { get; set; }
    }
}
