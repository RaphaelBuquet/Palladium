﻿Imports LogViewerVB.Core
Imports LogDataStore = LogViewerVB.WinForms.Logging.LogDataStore

Namespace DataStores

    ' Application-wide shared instance of the LogDataStore logging entries
    Public Module MainControlsDataStore

        Public Property DataStore As ILogDataStore = New LogDataStore()

    End Module

End Namespace