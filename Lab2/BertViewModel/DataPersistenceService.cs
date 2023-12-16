using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BertViewModel
{
    public class PersistenceService
    {
        private readonly string _filePath = "appstate.json";

        public AppState LoadState()
        {
            if (!File.Exists(_filePath))
            {
                return new AppState();
            }

            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<AppState>(json);
        }

        public void SaveState(AppState state)
        {
            string json = JsonConvert.SerializeObject(state);
            File.WriteAllText(_filePath, json);
        }
    }
}
