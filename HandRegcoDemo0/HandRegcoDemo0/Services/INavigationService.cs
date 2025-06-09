using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandRegcoDemo0.Services
{
    public interface INavigationService
    {
        void OpenCameraView();
        void OpenVideoRecorderView();
        void ExitApplication();
    }
}
