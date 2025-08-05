using Life;
using Life.DB;
using Life.Network;
using Life.UI;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GrayCard581
{
    public class Main : Plugin
    {
        public SQLiteAsyncConnection db { get => LifeDB.db; }
        public string directoryPath;
        public string configPath;
        public Config config;

        public Main(IGameAPI api) : base(api) { }

        public override async void OnPluginInit()
        {
            base.OnPluginInit();
            directoryPath = Path.Combine(pluginsPath, Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            configPath = Path.Combine(directoryPath, "config.json");
            if (!File.Exists(configPath))
            {
                config = new Config();
                File.WriteAllText(configPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
            }
            else
            {
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            }
            await db.CreateTableAsync<GrayCard>();
            Nova.server.OnMinutePassedEvent += () =>
            {
                foreach (var player in Nova.server.Players.Where(obj => obj.isSpawned).ToList())
                {
                    if (player.GetVehicleId() > 0)
                    {
                        if (player.setup.driver.seatId == 0)
                        {
                            var query = db.Table<GrayCard>().Where(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId).FirstOrDefaultAsync().Result;
                            if (query != null && query.IsExpired())
                            {
                                player.Notify("GrayCard581", "La carte grise de ce véhicule est expirée. Vous êtes dans l'illégalité !", NotificationManager.Type.Warning);
                            }
                            else if (query == null)
                            {
                                player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise. Vous êtes dans l'illégalité !", NotificationManager.Type.Warning);
                            }
                        }
                    }
                }
            };
            new SChatCommand("/graycard", new string[] { "/cartegrise", "/cg", "/gc" }, "Ouvre le menu des cartes grises", "/graycard", (player, args) =>
            {
                OpenMenu(player);
            }).Register();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{Assembly.GetExecutingAssembly().GetName().Name} initialise !");
            Console.ResetColor();
        }

        public void OpenMecanicMenu(Player player)
        {
            if (config.bizId > 0)
            {
                if (player.character.BizId != config.bizId)
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas autorisé a faire des Carte Grises.", NotificationManager.Type.Error);
                    return;
                }
            }
            var panel = new UIPanel("Gestion Carte Grise", UIPanel.PanelType.Tab);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui => ui.SelectTab());
            panel.AddButton("Retour", ui => OpenMenu(player));
            panel.AddTabLine("Vérifier la Carte Grise", async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var query = await db.Table<GrayCard>().Where(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId).FirstOrDefaultAsync();
                    if (query != null)
                    {
                        if (query.IsExpired())
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est expirée.", NotificationManager.Type.Warning);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est valide.", NotificationManager.Type.Success);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            panel.AddTabLine("Créer une Carte Grise", async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    var query = db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicle.VehicleDbId).FirstOrDefaultAsync().Result;
                    if (query != null)
                    {
                        player.Notify("GrayCard581", "Ce véhicule a déjà une carte grise.", NotificationManager.Type.Error);
                    }
                    else
                    {
                        var instance = new GrayCard
                        {
                            VehicleId = vehicle.VehicleDbId,
                            Date = DateTime.Now
                        };
                        await db.InsertAsync(instance);
                        player.Notify("GrayCard581", "La carte grise a été créée avec succès.", NotificationManager.Type.Success);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            panel.AddTabLine("Supprimer une Carte Grise", async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    if (vehicle.VehicleDbId > 0)
                    {
                        var query = await db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicle.VehicleDbId).FirstOrDefaultAsync();
                        if (query != null)
                        {
                            await db.DeleteAsync(query);
                            player.Notify("GrayCard581", "La carte grise a été supprimée avec succès.", NotificationManager.Type.Success);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            panel.AddTabLine("Actualiser la Carte Grise", ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    var query = db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicle.VehicleDbId).FirstOrDefaultAsync().Result;
                    if (query != null)
                    {
                        if (query.IsExpired())
                        {
                            query.Date = DateTime.Now;
                            db.UpdateAsync(query);
                            player.Notify("GrayCard581", "La carte grise a été actualisée avec succès.", NotificationManager.Type.Success);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule n'est pas expirée.", NotificationManager.Type.Error);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            player.ShowPanelUI(panel);
        }

        public void OpenLawEnforcementMenu(Player player)
        {
            var panel = new UIPanel("Contrôle Carte Grise", UIPanel.PanelType.Tab);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui => ui.SelectTab());
            panel.AddButton("Retour", ui => OpenMenu(player));
            panel.AddTabLine($"Contrôler la Carte Grise", async ui =>
            {
                int vehicleId = 0;
                var targetVehicle = player.GetClosestVehicle();
                if (targetVehicle != null)
                {
                    vehicleId = targetVehicle.VehicleDbId;
                }
                else
                {
                    var targetFakeVehicle = player.GetClosestFakeVehicle();
                    if (targetFakeVehicle != null)
                    {
                        vehicleId = targetFakeVehicle.vehicleDbId;
                    }
                }
                if (vehicleId > 0)
                {
                    var query = await db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicleId).FirstOrDefaultAsync();
                    if (query != null)
                    {
                        if (query.IsExpired())
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est expirée.", NotificationManager.Type.Warning);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est valide.", NotificationManager.Type.Success);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            player.ShowPanelUI(panel);
        }

        public void OpenMenu(Player player)
        {
            var panel = new UIPanel("Carte Grise", UIPanel.PanelType.Tab);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui => ui.SelectTab());
            panel.AddTabLine("Carte Grises", ui => OpenGrayCardsMenu(player));
            panel.AddTabLine("Vérifier la Catre Grise", async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var query = await db.Table<GrayCard>().Where(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId).FirstOrDefaultAsync();
                    if (query != null)
                    {
                        if (query.IsExpired())
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est expirée.", NotificationManager.Type.Warning);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est valide.", NotificationManager.Type.Success);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                    }
                }
                else
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
            });
            if (player.GetActivities().Contains(Life.BizSystem.Activity.Type.Mecanic))
            {
                panel.AddTabLine("Gestion", ui =>
                {
                    OpenMecanicMenu(player);
                });
            }
            if (player.GetActivities().Contains(Life.BizSystem.Activity.Type.LawEnforcement))
            {
                panel.AddTabLine("Contrôle", ui =>
                {
                    OpenLawEnforcementMenu(player);
                });
            }
            if (player.account.adminLevel > 0 && player.serviceAdmin)
            {
                panel.AddTabLine("Admin", ui =>
                {
                    OpenAdminMenu(player);
                });
            }
            player.ShowPanelUI(panel);
        }

        public void OpenAdminMenu(Player player)
        {
            var panel = new UIPanel("Gestion Carte Grise (Admin)", UIPanel.PanelType.Tab);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Sélectionner", ui => ui.SelectTab());
            panel.AddButton("Retour", ui => OpenMenu(player));
            panel.AddTabLine("Vérifier la Carte Grise", async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var query = await db.Table<GrayCard>().Where(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId).FirstOrDefaultAsync();
                    if (query != null)
                    {
                        if (query.IsExpired())
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est expirée.", NotificationManager.Type.Warning);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule est valide.", NotificationManager.Type.Success);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            panel.AddTabLine("Créer une Carte Grise", async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    var query = db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicle.VehicleDbId).FirstOrDefaultAsync().Result;
                    if (query != null)
                    {
                        player.Notify("GrayCard581", "Ce véhicule a déjà une carte grise.", NotificationManager.Type.Error);
                    }
                    else
                    {
                        var instance = new GrayCard
                        {
                            VehicleId = vehicle.VehicleDbId,
                            Date = DateTime.Now
                        };
                        await db.InsertAsync(instance);
                        player.Notify("GrayCard581", "La carte grise a été créée avec succès.", NotificationManager.Type.Success);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            panel.AddTabLine("Supprimer une Carte Grise", async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    if (vehicle.VehicleDbId > 0)
                    {
                        var query = await db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicle.VehicleDbId).FirstOrDefaultAsync();
                        if (query != null)
                        {
                            await db.DeleteAsync(query);
                            player.Notify("GrayCard581", "La carte grise a été supprimée avec succès.", NotificationManager.Type.Success);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            panel.AddTabLine("Actualiser la Carte Grise", ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    var query = db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicle.VehicleDbId).FirstOrDefaultAsync().Result;
                    if (query != null)
                    {
                        if (query.IsExpired())
                        {
                            query.Date = DateTime.Now;
                            db.UpdateAsync(query);
                            player.Notify("GrayCard581", "La carte grise a été actualisée avec succès.", NotificationManager.Type.Success);
                        }
                        else
                        {
                            player.Notify("GrayCard581", "La carte grise de ce véhicule n'est pas expirée.", NotificationManager.Type.Error);
                        }
                    }
                    else
                    {
                        player.Notify("GrayCard581", "Ce véhicule n'a pas de carte grise.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("GrayCard581", "Vous n'êtes pas dans un véhicule.", NotificationManager.Type.Error);
                }
            });
            player.ShowPanelUI(panel);
        }

        public async void OpenGrayCardsMenu(Player player)
        {
            var panel = new UIPanel("Carte Grises", UIPanel.PanelType.TabPrice);
            panel.AddButton("Fermer", ui => player.ClosePanel(ui));
            panel.AddButton("Retour", ui => OpenMenu(player));
            foreach (var vehicle in Nova.v.vehicles.Where(obj => obj.permissions.owner.characterId == player.character.Id).ToList())
            {
                var query = await db.Table<GrayCard>().Where(obj => obj.VehicleId == vehicle.vehicleId).FirstOrDefaultAsync();
                panel.AddTabLine(vehicle.plate + "<br>" + "<i>" + Nova.v.vehicleModels[vehicle.modelId].VehicleName + "</i>",
                    (query != null ? $"<color={LifeServer.COLOR_GREEN}>Valide</color>" : $"<color={LifeServer.COLOR_RED}>Non valide</color>"),
                    GetVehicleIconId(vehicle.modelId),
                    ui => { });
            }
            player.ShowPanelUI(panel);
        }

        public static int GetVehicleIconId(int modelId)
        {
            var model = Nova.v.vehicleModels[modelId];
            if (model.Icon == null)
                return -1;
            var iconId = Array.IndexOf(Nova.man.newIcons.ToArray(), model.Icon);
            if (iconId > 0)
                return iconId;
            else
                return -1;
        }
    }
}
