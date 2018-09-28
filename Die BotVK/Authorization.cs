using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace Die_BotVK
{
    public class Authorization
    {

        static Program main = new Program();
        static ColorConsole colorConsole = new ColorConsole();

        public bool Auth(string LoginA, string PasswordA, ulong ID, bool HashCode)
        {
            try
            {
                switch (HashCode)
                {
                    case true:
                        {
                            Program.vkApi.Authorize(new ApiAuthParams
                            {
                                ApplicationId = ID,
                                Login = LoginA,
                                Password = PasswordA,
                                Settings = Settings.All,
                                TwoFactorAuthorization = DoubleCode
                            });
                        }
                        break;

                    case false:
                        {
                            Program.vkApi.Authorize(new ApiAuthParams
                            {
                                ApplicationId = ID,
                                Login = LoginA,
                                Password = PasswordA,
                                Settings = Settings.All
                            });
                        }
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Func<string> DoubleCode = () =>
        {
            colorConsole.ColorWrite("Введите код авторизации:", ConsoleColor.White, ConsoleColor.White);
            return Console.ReadLine();
        };

    }
}
