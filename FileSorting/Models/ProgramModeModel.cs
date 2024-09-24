using CommunityToolkit.Mvvm.ComponentModel;
using FileSorting.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorting.Models
{
    public class ProgramModeModel : ObservableObject
    {
        private string description;
        public string Description 
        {
            get => description;
            set => SetProperty(ref description, value); 
        }

        private ProgramMode mode;
        public ProgramMode Mode
        {
            get => mode;
            set => SetProperty(ref mode, value);
        }
    }
}
