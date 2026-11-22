namespace HRRagSystem.Models
{
    public class DocumentChangesSet
    {
        public List<DocumentInfo> NewDocuments { get; set; } = new List<DocumentInfo>();
        public List<DocumentInfo> UpdatedDocuments { get; set; } = new List<DocumentInfo>();
        public List<DocumentInfo> DeletedDocuments { get; set; } = new List<DocumentInfo>();
    }

}
