using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorting.Models
{
    public class MessageModel:ObservableObject
    {
        public MessageModel()
        {
            updateTime = DateTime.Now;
            content = string.Empty;
        }

        private DateTime updateTime;
        public DateTime UpdateTime 
        {
            get => updateTime;
            set => SetProperty(ref updateTime, value);  
        }

        private string content;
        public string Content
        {
            get => content;
            set => SetProperty(ref content, value);
        }
    }
}
