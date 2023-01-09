using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.message
{
    public class DataSerializer
    {
        private ILog _log;
        public DataSerializer()
        {
            _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public string SerializePlaceOrderSet(int orderSetId, CommandConstants.OrderSetCategory category)
        {
            return orderSetId.ToString() + "," + category.ToString();
        }

        public Tuple<int, CommandConstants.OrderSetCategory> DeserializePlaceOrderSet(string data)
        {
            try {
                string[] spData = data.Split(',');
                int orderSetId = Int32.Parse(spData[0]);
                CommandConstants.OrderSetCategory category = (CommandConstants.OrderSetCategory) Enum.Parse(typeof(CommandConstants.OrderSetCategory), spData[1]);
                return new Tuple<int, CommandConstants.OrderSetCategory>(orderSetId, category);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

            return null;
        }
    }
}
