using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBase.Files.Models
{
    public class BlazorBaseFileOptions
    {
        public static BlazorBaseFileOptions Instance { get; private set; }

        #region Members

        protected readonly IServiceProvider ServiceProvider;

        protected readonly Action<BlazorBaseFileOptions> ConfigureOptions;

        #endregion

        #region Constructors
        public BlazorBaseFileOptions(IServiceProvider serviceProvider, Action<BlazorBaseFileOptions> configureOptions)
        {
            ServiceProvider = serviceProvider;
            ConfigureOptions = configureOptions;

            ConfigureOptions?.Invoke(this);

            SetInstance();
        }

        public void SetInstance()
        {
            Instance = this;
        }
        #endregion

        #region Properties
        public string FileStorePath { get; set; } = @"C:\BlazorBaseFileStore";
        public string TempFileStorePath { get; set; } = @"C:\BlazorBaseFileStore\Temp";
        public bool AutomaticallyDeleteOldTemporaryFiles { get; set; } = true;
        public uint DeleteTemporaryFilesOlderThanXSeconds { get; set; } = 60 * 60 * 24 * 7; // 7 days
        #endregion


    }
}
