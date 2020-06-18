using Microsoft.Extensions.Logging;
using System;

namespace Innovent_BL
{
    public class SimLogic
    {
        public SimLogic(ILogger logger)
        {
            logger.LogInformation("HELLO FROM SIMLOGIC");
        }
    }
}
