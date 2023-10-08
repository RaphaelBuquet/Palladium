#include <windows.h>
#include <shobjidl.h>

extern "C" __declspec(dllexport) HRESULT CreateShellLink(LPCWSTR lpszShortcutPath, LPCWSTR lpszFilePath,
                                                         LPCWSTR lpszArgs = nullptr)
{
    HRESULT hres;
    IShellLink* psl;

    // Get a pointer to the IShellLink interface.
    hres = CoCreateInstance(CLSID_ShellLink, nullptr, CLSCTX_INPROC_SERVER, __uuidof(IShellLink),
                            reinterpret_cast<void**>(&psl));

    if (SUCCEEDED(hres))
    {
        IPersistFile* ppf;

        // Set the path to the shortcut target and set the arguments.
        psl->SetPath(lpszFilePath);
        if (lpszArgs != nullptr)
        {
            psl->SetArguments(lpszArgs);
        }

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

extern "C" __declspec(dllexport) HRESULT GetShellLinkArguments(LPCWSTR lpszShortcutPath, LPWSTR lpszArgs, int nArgSize)
{
    HRESULT hres;
    IShellLink* psl;

    // Get a pointer to the IShellLink interface.
    hres = CoCreateInstance(CLSID_ShellLink, nullptr, CLSCTX_INPROC_SERVER, __uuidof(IShellLink),
                            reinterpret_cast<void**>(&psl));

    if (SUCCEEDED(hres))
    {
        IPersistFile* ppf;

        // Query IShellLink for the IPersistFile interface for saving the
        // shortcut in persistent storage.
        hres = psl->QueryInterface(__uuidof(IPersistFile), reinterpret_cast<void**>(&ppf));

        if (SUCCEEDED(hres))
        {
            // Load the link by calling IPersistFile::Load.
            hres = ppf->Load(lpszShortcutPath, STGM_READ);

            if (SUCCEEDED(hres))
            {
                // Get the Arguments
                hres = psl->GetArguments(lpszArgs, nArgSize);
            }
            ppf->Release();
        }
        psl->Release();
    }
    return hres;
}
