using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace AIOHud
{
    public class LeHud : BaseScript
    {

        private bool _cruising;
        private float _rpm;
        private readonly Control _cruiseKey;
        private int _gear;
        private bool UIOpen;

        public LeHud()
        {
            _cruiseKey = Control.SelectCharacterTrevor; //F7
            Tick += CarHud;
            Tick += OnCruise;
            EventHandlers["onClientResourceStart"] += new Action<string>(ResStart);
        }

        private List<VehicleClass> _noCruiseForYou = new List<VehicleClass>
        {
            VehicleClass.Cycles,
            VehicleClass.Motorcycles,
            VehicleClass.Planes,
            VehicleClass.Helicopters,
            VehicleClass.Boats,
            VehicleClass.Trains
        };

        public void ResStart(string resName)
        {
            if (GetCurrentResourceName() != resName) return;
        }
        public async Task OnCruise()
        {
            {
                Vehicle v = Game.PlayerPed?.CurrentVehicle;

                if (v != null)
                {
                    Game.DisableControlThisFrame(0, _cruiseKey);

                    if ((Game.IsDisabledControlJustReleased(0, _cruiseKey) || Game.IsControlJustReleased(0, _cruiseKey)) &&
                        v.CurrentGear != 0 && !_noCruiseForYou.Contains(v.ClassType))
                    {
                        _cruising = !_cruising;

                        if (_cruising)
                        {
                            CruiseAtSpeed(v.Speed);
                            _rpm = v.CurrentRPM;
                            _gear = v.CurrentGear;
                        }
                    }
                }
                else if (_cruising)
                {
                    _cruising = false;
                }
                else
                {
                    await Delay(100);
                }
            }
        }

        private async void CruiseAtSpeed(float s)
        {
            while (_cruising)
            {
                Vehicle v = Game.PlayerPed?.CurrentVehicle;

                if (v != null)
                {
                    v.Speed = s;
                    v.CurrentRPM = _rpm;

                    if (v.Driver == null || v.Driver != Game.PlayerPed || v.IsInWater || v.IsInBurnout || !v.IsEngineRunning ||
                        v.IsInAir || v.HasCollided ||
                        GTASpeedToMPH(v.Speed) <= 25f || GTASpeedToMPH(v.Speed) >= 100f ||
                        HaveAnyTiresBurst(v) ||
                        Game.IsControlPressed(0, Control.VehicleHandbrake) || Game.IsDisabledControlPressed(0, Control.VehicleHandbrake) ||
                        Game.IsControlPressed(0, Control.VehicleBrake) || Game.IsDisabledControlPressed(0, Control.VehicleBrake))
                    { 
                        _cruising = false;
                    }

                    if (Game.IsControlPressed(0, Control.VehicleAccelerate) ||
                        Game.IsDisabledControlPressed(0, Control.VehicleAccelerate))
                    {
                        AcceleratingToNewSpeed();
                    }
                }
                else
                {
                    return;
                }

                await Delay(0);
            }
        }

        private async void AcceleratingToNewSpeed()
        {
            _cruising = false;

            while ((Game.IsControlPressed(0, Control.VehicleAccelerate) ||
                   Game.IsDisabledControlPressed(0, Control.VehicleAccelerate)) &&
                   Game.PlayerPed.CurrentVehicle != null)
            {
                await Delay(100);
            }

            if (Game.PlayerPed.CurrentVehicle != null)
            {
                _cruising = true;
                CruiseAtSpeed(Game.PlayerPed.CurrentVehicle.Speed);
            }
        }

        private float GTASpeedToMPH(float s)
        {
            return s * 2.23694f + 0.5f;
        }


        private bool HaveAnyTiresBurst(Vehicle v)
        {
            List<bool> tiresBurst = new List<bool>();

            for (int i = 0; i < 48; i++)
            {
                if (i == 6)
                {
                    i = 45;
                }

                if (i == 46)
                {
                    i = 47;
                }

                tiresBurst.Add(API.IsVehicleTyreBurst(v.Handle, i, false));
            }

            return tiresBurst.Contains(true);
        }

     
        public async Task CarHud()
        {
            if (IsPedInAnyVehicle(PlayerPedId(), true))
            {
                int veh = GetVehiclePedIsIn(GetPlayerPed(-1), false);

                if (GetPedInVehicleSeat(veh, -1) == PlayerPedId())
                {
                    string fuelLvl = Game.PlayerPed.CurrentVehicle.FuelLevel.ToString();
                    double speed = 0;
                    if (GetEntitySpeed(veh) != 0)
                    {
                        speed = GetEntitySpeed(veh) * 2.236936;
                    }
                    string leSpeed = speed.ToString();
                    string RPM = GetVehicleCurrentRpm(veh).ToString();
                    string gear = GetVehicleCurrentGear(veh).ToString();
                    if (int.Parse(gear) == 0) gear = "R";
                    if (gear == null || gear.Length == -1) gear = "N";
                    if (leSpeed != "0") leSpeed = leSpeed.Substring(0, leSpeed.IndexOf("."));
                    if (leSpeed == "0") leSpeed = "00";
                    if (RPM != "0") RPM = RPM.Substring(2, RPM.IndexOf("."));
                    if (RPM == "0") RPM = "0.0";
                    if (fuelLvl != "0" || float.Parse(fuelLvl) != 0) fuelLvl = fuelLvl.Substring(0, fuelLvl.IndexOf("."));
                    if (fuelLvl == "0") fuelLvl = "E";
                    if (_cruising)
                    {
                        gear = _gear.ToString();
                        DrawText2D(0.122f, 0.745f, $"Cruise", 0.45F, false);
                        DrawText2D(0.122f, 0.765f, $"~g~Cntrl", 0.65F, false);
                        DrawText2D(0.02f, 0.765f, $"{fuelLvl}", 0.65F, false);
                        DrawText2D(0.02f, 0.745f, $"Fuel", 0.45F, false);
                        DrawText2D(0.094f, 0.765f, $"{leSpeed}", 0.65F, false);
                        DrawText2D(0.094f, 0.745f, $"MPH", 0.45F, false);
                        DrawText2D(0.069f, 0.765f, $"~r~LCK", 0.65F, false);
                        DrawText2D(0.069f, 0.745f, $"RPM", 0.45F, false);
                        DrawText2D(0.049f, 0.765f, $"{_gear}", 0.65F, false);
                        DrawText2D(0.043f, 0.745f, $"Gear", 0.45F, false);
                    }
                    else
                    {
                        DrawText2D(0.122f, 0.745f, $"Cruise", 0.45F, false);
                        DrawText2D(0.122f, 0.765f, $"Cntrl", 0.65F, false);
                        DrawText2D(0.02f, 0.765f, $"{fuelLvl}", 0.65F, false);
                        DrawText2D(0.02f, 0.745f, $"Fuel", 0.45F, false);
                        DrawText2D(0.094f, 0.765f, $"{leSpeed}", 0.65F, false);
                        DrawText2D(0.094f, 0.745f, $"MPH", 0.45F, false);
                        DrawText2D(0.069f, 0.765f, $"{RPM}", 0.65F, false);
                        DrawText2D(0.069f, 0.745f, $"RPM", 0.45F, false);
                        DrawText2D(0.049f, 0.765f, $"{gear}", 0.65F, false);
                        DrawText2D(0.043f, 0.745f, $"Gear", 0.45F, false);
                    }

                }
            }
        }

        private void DrawText2D(float x, float y, string text, float scale, bool center)
        {
            SetTextFont(4);
            SetTextProportional(false);
            SetTextScale(scale, scale);
            SetTextColour(255, 255, 255, 255);
            SetTextDropshadow(0, 0, 0, 0, 255);
            SetTextDropShadow();
            SetTextEdge(4, 0, 0, 0, 255);
            SetTextOutline();
            if (center == true)
            {
                SetTextJustification(0);
            }
            SetTextEntry("STRING");
            AddTextComponentString(text);
            DrawText(x, y);
        }

    }
}
