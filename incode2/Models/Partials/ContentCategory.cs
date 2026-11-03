using Microsoft.AspNetCore.Mvc;

namespace incode2.Models;
//這個partial是要套用下面這行的屬性類別
[ModelMetadataType(typeof(ContentCategoryMetadata))]
public partial class ContentCategory
{

}

