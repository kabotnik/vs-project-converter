namespace vs_project_converter
{
    public interface IConverter
    {
        void ConvertAndSave(bool overwrite = false);
    }
}