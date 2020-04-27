namespace Voxels.Common.Interfaces
{
    public interface INeedInitializeOnWorldSizeChange
    {
        /// <summary>
        /// New map size values are taken from the <see cref="GlobalVariables"/> class.
        /// </summary>
        void InitializeOnWorldSizeChange();
    }
}
