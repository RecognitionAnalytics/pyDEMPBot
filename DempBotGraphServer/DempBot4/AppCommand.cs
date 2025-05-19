namespace Dempbot4
{
  using System.Windows.Input;

  public class AppCommand
  {
    #region CommandFramework Fields
    private static RoutedUICommand loadFile;

    private static RoutedUICommand pinUnpin;
    private static RoutedUICommand addMruEntry;
    private static RoutedUICommand removeMruEntry;
    #endregion CommandFramework Fields

    #region Static Constructor (Constructs static application commands)
    /// <summary>
    /// Define custom commands and their key gestures
    /// </summary>
    static AppCommand()
    {
      InputGestureCollection inputs = null;

      // Initialize pin command (to set or unset a pin in MRU and re-sort list accordingly)
      inputs = new InputGestureCollection();
      AppCommand.pinUnpin = new RoutedUICommand("Pin or Unpin", "Pin", typeof(AppCommand), inputs);

      // Execute add recent files list etnry pin command (to add another MRU entry into the list)
      inputs = new InputGestureCollection();
      AppCommand.addMruEntry = new RoutedUICommand("Add Entry", "AddEntry", typeof(AppCommand), inputs);

      // Execute remove pin command (remove a pin from a recent files list entry)
      inputs = new InputGestureCollection();
      AppCommand.removeMruEntry = new RoutedUICommand("Remove Entry", "RemoveEntry", typeof(AppCommand), inputs);

      // Execute file open command (without user interaction)
      inputs = new InputGestureCollection();
      AppCommand.loadFile = new RoutedUICommand("Open ...", "LoadFile", typeof(AppCommand), inputs);
    }
    #endregion Static Constructor

    #region CommandFramwork Properties (Exposes Commands to which the UI can bind to)
    /// <summary>
    /// Execute pin/unpin command (to set or unset a pin in MRU and re-sort list accordingly)
    /// </summary>
    public static RoutedUICommand PinUnpin
    {
      get { return AppCommand.pinUnpin; }
    }

    /// <summary>
    /// Execute add recent files list etnry pin command (to add another MRU entry into the list)
    /// </summary>
    public static RoutedUICommand AddMruEntry
    {
      get { return AppCommand.addMruEntry; }
    }

    /// <summary>
    /// Execute remove pin command (remove a pin from a recent files list entry)
    /// </summary>
    public static RoutedUICommand RemoveMruEntry
    {
      get { return AppCommand.removeMruEntry; }
    }

    /// <summary>
    /// Execute file open command (without user interaction)
    /// </summary>
    public static RoutedUICommand LoadFile
    {
      get { return AppCommand.loadFile; }
    }
    #endregion CommandFramwork_Properties
  }
}
