using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Life;
using Life.Network;
using Life.UI;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using Unity.Collections;
using UnityEngine.Rendering;
using Utils;
using Format = ModKit.Helper.TextFormattingHelper;

namespace GreyCard581
{
    public class Main : ModKit.ModKit
    {
        public static Main instance;
        public static string SourceName = "Shape581";
        public static string Version = "1.0.0";
        public static string LogInfo = $"[{SourceName} - V{Version}]";

        public Main(IGameAPI api) : base(api)
        {
            PluginInformations = new ModKit.Interfaces.PluginInformations(AssemblyHelper.GetName(), Version, SourceName);
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            instance = this;
            Orm.RegisterTable<GreyCard_Biz>();
            Orm.RegisterTable<GreyCard_Data>();
            ModKit.Internal.Logger.LogSuccess(LogInfo, "Initialisé");
            AAMenu.Menu.AddDocumentTabLine(PluginInformations, Format.Color("Certificat d'immatriculation", Format.Colors.Info), aaMenu =>
            {
                var player = PanelHelper.ReturnPlayerFromPanel(aaMenu);
                OpenMenu(player, player);
            });
            AAMenu.Menu.AddBizTabLine(PluginInformations, new List<Life.BizSystem.Activity.Type> { Life.BizSystem.Activity.Type.Mecanic }, null, Format.Color("Gestion Certificat d'immatriculation", Format.Colors.Info), async aaMenu =>
            {
                var player = PanelHelper.ReturnPlayerFromPanel(aaMenu);
                var query = await GreyCard_Biz.Query(obj => obj.BizId == player.biz.Id);
                if (query.Any())
                {
                    ManageMenu(player);
                }
                else
                {
                    AAMenu.AAMenu.menu.BizPanel(player);
                    player.Notify(Format.Color("Erreur", Format.Colors.Error), "Votre entreprise n'est pas agrée pour faire les Ceritificat d'immatriculation", NotificationManager.Type.Error);
                }
            });
            AAMenu.Menu.AddAdminTabLine(PluginInformations, 1, Format.Color("Gestion Entreprise Carte Grise", Format.Colors.Info), aaMenu =>
            {
                var player = PanelHelper.ReturnPlayerFromPanel(aaMenu);
                var panel = PanelHelper.Create(Format.Color($"Gestion Plugin GreyCard581", Format.Colors.Info), UIPanel.PanelType.TabPrice, player, () => OnPluginInit());
                panel.CloseButton();
                panel.AddButton(Format.Color("Séléctionner", Format.Colors.Success), ui => ui.SelectTab());
                panel.PreviousButton();
                panel.AddTabLine(Format.Align("Ajouter une entreprise", Format.Aligns.Center), ui =>
                {
                    AddBiz(player);
                });
                panel.Display();
            });
            AAMenu.Menu.AddBizTabLine(PluginInformations, new List<Life.BizSystem.Activity.Type> { Life.BizSystem.Activity.Type.LawEnforcement }, null, Format.Color("Verifier le Certificat d'immatriculation", Format.Colors.Info), async aaMenu =>
            {
                var player = PanelHelper.ReturnPlayerFromPanel(aaMenu);
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    var lifeVehicle = Nova.v.GetVehicle(vehicle.VehicleDbId);
                    var query = await GreyCard_Data.Query(obj => obj.VehicleId == lifeVehicle.vehicleId);
                    GreyCard_Data element = default;
                    foreach (var elements in query)
                    {
                        element = elements;
                        break;
                    }
                    if (query.Any())
                    {
                        if (!GreyCard_Data.IsExpired(element.DateOfExpiration))
                        {
                            BetterInfo(player, element);
                        }
                        else
                        {
                            player.Notify(Format.Color("Erreur", Format.Colors.Error), "Le Certificat d'immatriculation est expirée.", NotificationManager.Type.Error);
                        }
                    }
                    else
                    {
                        player.Notify(Format.Color("Erreur", Format.Colors.Error), "Ce véhicule n'a pas de Cartificat d'immatriculation.", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify(Format.Color("Erreur", Format.Colors.Error), "Vous devez être dans un véhicule.", NotificationManager.Type.Error);
                }
            });
        }

        public override async void OnPlayerEnterVehicle(Vehicle vehicle, int seatId, Player player)
        {
            base.OnPlayerEnterVehicle(vehicle, seatId, player);
            var query = await GreyCard_Data.Query(obj => obj.VehicleId == vehicle.VehicleDbId);
            if (!query.Any())
            {
                player.Notify(Format.Color("Avertissement", Format.Colors.Warning), "Vous n'avez pas de Certificat d'immatriculation", NotificationManager.Type.Warning);
            }
        }

        public async void AddBiz(Player player)
        {
            var panel = PanelHelper.Create(Format.Color("Ajouter une entreprise", Format.Colors.Success), UIPanel.PanelType.TabPrice, player, () => AddBiz(player));
            panel.PreviousButton("Annuler");
            panel.AddButton(Format.Color("Séléctionner", Format.Colors.Success), ui =>
            {
                ui.SelectTab();
            });
            foreach (var bizs in Nova.biz.bizs)
            {
                var query = await GreyCard_Biz.Query(obj => obj.BizId == bizs.Id);
                if (!query.Any())
                {
                    panel.AddTabLine(Format.Color(bizs.BizName, Format.Colors.Info), async ui =>
                    {
                        await GreyCard_Biz.Add(bizs.Id);
                        player.Notify(Format.Color("Succès", Format.Colors.Success), "Vous avez ajouter cette entreprise.", NotificationManager.Type.Success);
                        panel.Close();
                    });
                }
            }
            panel.Display();
        }

        public void ManageMenu(Player player)
        {
            var panel = PanelHelper.Create(Format.Color("Gestion Certificat d'immatriculation", Format.Colors.Info), UIPanel.PanelType.TabPrice, player, () => ManageMenu(player));
            panel.CloseButton();
            panel.AddButton(Format.Color("Séléctionner", Format.Colors.Success), ui => ui.SelectTab());
            panel.PreviousButton();
            panel.AddTabLine(Format.Align(Format.Color("Crée un Cartificat d'immatriculation", Format.Colors.Success), Format.Aligns.Center), async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var vehicle = player.setup.driver.vehicle;
                    var lifeVehicle = Nova.v.GetVehicle(vehicle.VehicleDbId);
                    var query = await GreyCard_Data.Query(obj => obj.VehicleId == lifeVehicle.vehicleId);
                    if (!query.Any())
                    {
                        await GreyCard_Data.Add(player, lifeVehicle);
                        player.Notify(Format.Color("Succès", Format.Colors.Success), "Vous avez crée la carte grise de ce véhicule.", NotificationManager.Type.Success);
                        panel.Close();
                    }
                    else
                    {
                        player.Notify(Format.Color("Erreur", Format.Colors.Error), "Ce véhicule possède déjà un Certificat d'immatriculation.", NotificationManager.Type.Error);
                        panel.Refresh();
                    }
                }
                else
                {
                    player.Notify(Format.Color("Erreur", Format.Colors.Error), "Vous devez être dans un véhicule.", NotificationManager.Type.Error);
                    panel.Close();
                }
            });
            panel.AddTabLine(Format.Color("Déchirer le Certificat d'immatriculation", Format.Colors.Error), async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var query = await GreyCard_Data.Query(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId);
                    if (query.Any())
                    {
                        foreach (var element in query)
                        {
                            ConfirmToDestoyGreyCard(player, element);
                        }
                    }
                    else
                    {
                        player.Notify(Format.Color("Erreur", Format.Colors.Error), "Ce véhicule ne possède pad de Certificat d'immatriculation.", NotificationManager.Type.Error);
                        panel.Refresh();
                    }
                }
                else
                {
                    player.Notify(Format.Color("Erreur", Format.Colors.Error), "Vous devez être dans un véhicule.", NotificationManager.Type.Error);
                    panel.Close();
                }
            });
            panel.AddTabLine(Format.Color("Mettre a jour le Certificat d'immatriculation", Format.Colors.Warning), async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var query = await GreyCard_Data.Query(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId);
                    if (query.Any())
                    {
                        var data = await GreyCard_Data.Get((int)player.setup.driver.vehicle.VehicleDbId);
                        if (GreyCard_Data.IsExpired(data.DateOfExpiration))
                        {
                            foreach (var element in query)
                            {
                                element.DateOfExpiration = GreyCard_Data.GetExpireDate();
                                await element.Save();
                                player.Notify(Format.Color("Succès", Format.Colors.Success), "Vous avez mis a jour le certificat d'immatriculation.", NotificationManager.Type.Success);
                                panel.Refresh();
                            }
                        }
                        else
                        {
                            player.Notify(Format.Color("Erreur", Format.Colors.Error), "Le Certificat d'immatriculation n'est pas expirée.", NotificationManager.Type.Error);
                            panel.Close();
                        }
                    }
                    else
                    {
                        player.Notify(Format.Color("Erreur", Format.Colors.Error), "Ce véhicule ne possède pad de Certificat d'immatriculation.", NotificationManager.Type.Error);
                        panel.Refresh();
                    }
                }
                else
                {
                    player.Notify(Format.Color("Erreur", Format.Colors.Error), "Vous devez être dans un véhicule.", NotificationManager.Type.Error);
                    panel.Close();
                }
            });
            panel.AddTabLine(Format.Color("Visioner les information", Format.Colors.Info), async ui =>
            {
                if (player.GetVehicleId() > 0)
                {
                    var query = await GreyCard_Data.Query(obj => obj.VehicleId == player.setup.driver.vehicle.VehicleDbId);
                    if (query.Any())
                    {
                        var data = await GreyCard_Data.Get((int)player.setup.driver.vehicle.VehicleDbId);
                        if (!GreyCard_Data.IsExpired(data.DateOfExpiration))
                        {
                            foreach (var element in query)
                            {
                                BetterInfo(player, element);
                            }
                        }
                        else
                        {
                            player.Notify(Format.Color("Erreur", Format.Colors.Error), "Le Certificat d'immatriculation est expirée.", NotificationManager.Type.Warning);
                            panel.Close();
                        }
                    }
                    else
                    {
                        player.Notify(Format.Color("Erreur", Format.Colors.Error), "Ce véhicule ne possède pad de Certificat d'immatriculation.", NotificationManager.Type.Error);
                        panel.Refresh();
                    }
                }
                else
                {
                    player.Notify(Format.Color("Erreur", Format.Colors.Error), "Vous devez être dans un véhicule.", NotificationManager.Type.Error);
                    panel.Close();
                }
            });
            panel.Display();
        }

        public void ConfirmToDestoyGreyCard(Player player, GreyCard_Data element)
        {
            var panel = PanelHelper.Create(Format.Color("Déchirer le Certificat d'immatriculation", Format.Colors.Error), UIPanel.PanelType.Text, player, () => ConfirmToDestoyGreyCard(player, element));
            panel.TextLines.Add("<b>Voulez vous vraiment déchirer le Certificat d'immatriculation ?</b>");
            panel.TextLines.Add("<b>Aucun retour en arrière sera possible.</b>");
            panel.AddButton("Annuler", ui => ManageMenu(player));
            panel.AddButton(Format.Color("Déchirer", Format.Colors.Error), async ui =>
            {
                await element.Delete();
                player.Notify(Format.Color("Succès", Format.Colors.Success), "Vous avez déchirer ce certificat d'immatriculation.", NotificationManager.Type.Success);
                ManageMenu(player);
            });
            panel.Display();
        }

        public async void OpenMenu(Player player, Player target)
        {
            var panel = PanelHelper.Create(Format.Color("Certificat d'immatriculation", Format.Colors.Info), UIPanel.PanelType.TabPrice, player, () => OpenMenu(player, target));
            panel.CloseButton();
            panel.AddButton(Format.Color("Séléctionner", Format.Colors.Success), ui => ui.SelectTab());
            panel.PreviousButton();
            foreach (var vehicles in Nova.v.vehicles)
            {
                if (vehicles.permissions.owner.characterId == target.character.Id)
                {
                    string statut = Format.Color("Invalide", Format.Colors.Error);
                    var query = await GreyCard_Data.Query(obj => obj.VehicleId == vehicles.vehicleId);
                    if (query.Any())
                    {
                        bool isExpired = false;
                        foreach (var elements in query)
                        {
                            if (GreyCard_Data.IsExpired(elements.DateOfExpiration))
                            {
                                statut = Format.Color("Expirer", Format.Colors.Warning);
                                isExpired = true;
                                break;
                            }
                        }
                        if (!isExpired)
                        {
                            statut = Format.Color("Valide", Format.Colors.Success);
                            panel.AddTabLine(Format.Color(VehicleUtils.GetModelNameByModelId(vehicles.modelId), Format.Colors.Info), statut, VehicleUtils.GetIconId(vehicles.modelId), ui =>
                            {
                                foreach (var elements in query)
                                {
                                    if (elements.VehicleId == vehicles.vehicleId)
                                    {
                                        BetterInfo(player, elements);
                                    }
                                }
                            });
                        }
                        else
                        {
                            panel.AddTabLine(Format.Color(VehicleUtils.GetModelNameByModelId(vehicles.modelId), Format.Colors.Info), statut, VehicleUtils.GetIconId(vehicles.modelId), ui =>
                            {
                                panel.Refresh();
                                player.Notify(Format.Color("Erreur", Format.Colors.Error), "Cette carte grise est expirer", NotificationManager.Type.Error);
                            });
                            continue;
                        }
                    }
                    else
                    {
                        panel.AddTabLine(Format.Color(VehicleUtils.GetModelNameByModelId(vehicles.modelId), Format.Colors.Info), statut, VehicleUtils.GetIconId(vehicles.modelId), ui =>
                        {
                            panel.Refresh();
                            player.Notify(Format.Color("Erreur", Format.Colors.Error), "Vous ne possedez pas cette carte grise.", NotificationManager.Type.Error);
                        });
                        continue;
                    }
                }
            }
            if (panel.lines.Count == 0)
            {
                panel.AddTabLine(Format.Color("Vous n'avez aucun véhicule", Format.Colors.Error), ui => panel.Refresh());
            }
            panel.Display();
        }

        public void BetterInfo(Player player, GreyCard_Data element)
        {
            var panel = PanelHelper.Create(Format.Color("Information Certificat d'immatriculation", Format.Colors.Info), UIPanel.PanelType.TabPrice, player, () => BetterInfo(player, element));
            panel.CloseButton();
            panel.PreviousButton();
            var vehicle = Nova.v.GetVehicle(element.VehicleId);
            panel.AddTabLine($"Date de création : {Format.Color($"{element.DateOfCard}", Format.Colors.Info)}", ui => { });
            panel.AddTabLine($"Date de première mise en circulation : {Format.Color($"{element.DateOfFirst}", Format.Colors.Info)}", ui => { });
            panel.AddTabLine($"Date d'expiration : {Format.Color($"{element.DateOfExpiration}", Format.Colors.Info)}", ui => { });
            panel.AddTabLine($"Adresse : {Format.Color($"Terrain N°{element.AreaId}", Format.Colors.Info)}", ui => { });
            panel.AddTabLine($"Nombre de place assise : {Format.Color($"{element.SeatNumber}", Format.Colors.Info)}", ui => { });
            panel.AddTabLine($"Type de carburant : {Format.Color($"{element.SeatNumber}", Format.Colors.Info)}", ui => { });
            panel.AddTabLine($"Numéro de série : {Format.Color($"{vehicle.vehicleId}", Format.Colors.Info)}", ui => { });
            var owner = Nova.server.GetPlayer(vehicle.permissions.owner.characterId);
            panel.AddTabLine($"Propriétaire : {Format.Color($"{owner.GetFullName()}", Format.Colors.Info)}", ui => { });
            panel.AddTabLine(Format.Color($"Co Propriétaire", Format.Colors.Purple), ui => { });
            foreach (var coOwner in vehicle.permissions.coOwners)
            {
                var playerOfCoOwner = Nova.server.GetPlayer(coOwner.characterId);
                panel.AddTabLine(Format.Color($"- {playerOfCoOwner.GetFullName()}", Format.Colors.Success), ui => { });
            }
            if (vehicle.permissions.coOwners.Count == 0)
            {
                panel.AddTabLine(Format.Color("Aucun Co Propriétaire", Format.Colors.Error), ui => { });
            }
            panel.Display();
        }
    }
}
