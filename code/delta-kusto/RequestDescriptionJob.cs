namespace delta_kusto
{
    public class RequestDescriptionJob
    {
        public string? Current { get; set; }
 
        public string? Target { get; set; }
        
        public bool? FilePath { get; set; }
        
        public bool? FolderPath { get; set; }
        
        public bool? CsvPath { get; set; }
        
        public bool? UsePluralForms { get; set; }
        
        public bool? PushToConsole { get; set; }
        
        public bool? PushToCurrent { get; set; }
    }
}