using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerProject.Services
{
    internal class NotificationService
    {
        public event Action<string> ShowNotification;

        public void Notify(string message)
        {
            ShowNotification?.Invoke(message);
        }
    }
}
