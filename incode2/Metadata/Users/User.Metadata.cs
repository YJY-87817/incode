using incode2.Metadata.Users;
using incode2.Models;
using Microsoft.AspNetCore.Mvc;

namespace incode2.Models
{
    [ModelMetadataType(typeof(UserMetadata))]
    public partial class User
    {
    }
    public partial class UserLogin
    {
    }
}
