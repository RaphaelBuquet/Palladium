#include <stdbool.h>
#include <vector>
#include <windows.h>


std::vector<DISPLAY_DEVICE> GetDisplayDevices()
{
    std::vector<DISPLAY_DEVICE> devices;
    int adapterIndex = 0;
    bool successful = true;
    do
    {
        DISPLAY_DEVICE device;
        ZeroMemory(&device, sizeof(device));
        device.cb = sizeof(device);

        successful = EnumDisplayDevices(nullptr, adapterIndex, &device, 0);
        adapterIndex++;

        if (successful)
        {
            devices.push_back(device);
        }
    }
    while (successful);

    return devices;
}

extern "C" __declspec(dllexport) int DisableNonPrimaryDisplays()
{
    // get all devices
    const auto devices = GetDisplayDevices();

    std::vector<DISPLAY_DEVICE> devicesToDisable;
    for (auto device : devices)
    {
        // exclude non-active devices, exclude primary device
        if (device.StateFlags & DISPLAY_DEVICE_ACTIVE && !(device.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE))
        {
            devicesToDisable.push_back(device);
        }
    }

    for (const auto device : devicesToDisable)
    {
        DEVMODE devmode;
        ZeroMemory(&devmode, sizeof(devmode));
        devmode.dmSize = sizeof(devmode);

        // turn off
        // "To detach the monitor, set DEVMODE.dmFields to DM_POSITION but set DEVMODE.dmPelsWidth
        // and DEVMODE.dmPelsHeight to zero"
        devmode.dmFields = DM_POSITION | DM_PELSHEIGHT | DM_PELSWIDTH;

        // Use 0 for dwflags to mark these settings as temporary. 
        // We can later call this again to restore the settings. (see RestoreSettings).
        const int result = ChangeDisplaySettingsEx(device.DeviceName, &devmode, nullptr, 0, nullptr);
        if (result != 0)
        {
            return result;
        }
    }

    return 0;
}

extern "C" __declspec(dllexport) int RestoreSettings()
{
    // "Passing NULL for the lpDevMode parameter and 0 for the dwFlags parameter is the easiest way to return to
    // the default mode after a dynamic mode change."
    const int result = ChangeDisplaySettingsEx(nullptr, nullptr, nullptr, 0, nullptr);
    return result;
}
