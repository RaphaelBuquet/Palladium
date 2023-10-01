#include <windows.h>
#include <shobjidl.h>

extern "C" __declspec(dllexport) int CreateShellLink(LPCWSTR lpszShortcutPath, LPCWSTR lpszFilePath)
{
    HRESULT hres;
    IShellLink* psl;

    // Get a pointer to the IShellLink interface.
    hres = CoCreateInstance(CLSID_ShellLink, nullptr, CLSCTX_INPROC_SERVER, __uuidof(IShellLink),
                            reinterpret_cast<void**>(&psl));

    if (SUCCEEDED(hres))
    {
        IPersistFile* ppf;

        // Set the path to the shortcut target and set the description.
        psl->SetPath(lpszFilePath);

        // Query IShellLink for the IPersistFile interface for saving the
        // shortcut in persistent storage.
        hres = psl->QueryInterface(__uuidof(IPersistFile), reinterpret_cast<void**>(&ppf));

        if (SUCCEEDED(hres))
        {
            // Save the link by calling IPersistFile::Save.
            hres = ppf->Save(lpszShortcutPath, TRUE);
            ppf->Release();
        }
        psl->Release();
    }
    return hres;
}
