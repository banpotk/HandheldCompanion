using HandheldCompanion.Controllers;
using HandheldCompanion.Controls;
using HandheldCompanion.Devices;
using HandheldCompanion.Managers;
using HandheldCompanion.Misc;
using HandheldCompanion.Utils;
using iNKORE.UI.WPF.Controls;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static HandheldCompanion.Managers.ControllerManager;
using Page = System.Windows.Controls.Page;

namespace HandheldCompanion.Views.Pages;

/// <summary>
///     Interaction logic for Devices.xaml
/// </summary>
public partial class ControllerPage : Page
{
    // controllers vars
    private HIDmode controllerMode = HIDmode.NoController;
    private HIDstatus controllerStatus = HIDstatus.Disconnected;

    public ControllerPage()
    {
        InitializeComponent();

        SteamDeckPanel.Visibility = IDevice.GetCurrent() is SteamDeck ? Visibility.Visible : Visibility.Collapsed;

        // manage events
        SettingsManager.SettingValueChanged += SettingsManager_SettingValueChanged;
        ControllerManager.ControllerPlugged += ControllerPlugged;
        ControllerManager.ControllerUnplugged += ControllerUnplugged;
        ControllerManager.ControllerSelected += ControllerManager_ControllerSelected;
        ControllerManager.StatusChanged += ControllerManager_Working;
        ProfileManager.Applied += ProfileManager_Applied;
        VirtualManager.ControllerSelected += VirtualManager_ControllerSelected;
    }

    public ControllerPage(string Tag) : this()
    {
        this.Tag = Tag;
    }

    private void ProfileManager_Applied(Profile profile, UpdateSource source)
    {
        // UI thread (async)
        Application.Current.Dispatcher.Invoke(() =>
        {
            // disable emulated controller combobox if profile is not default or set to default controller
            if (!profile.Default && profile.HID != HIDmode.NotSelected)
            {
                cB_HidMode.IsEnabled = false;
                HintsHIDManagedByProfile.Visibility = Visibility.Visible;
            }
            else
            {
                cB_HidMode.IsEnabled = true;
                HintsHIDManagedByProfile.Visibility = Visibility.Collapsed;
            }
        });
    }
    private void SettingsManager_SettingValueChanged(string name, object value, bool temporary)
    {
        // UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            switch (name)
            {
                case "HIDcloakonconnect":
                    Toggle_Cloaked.IsOn = Convert.ToBoolean(value);
                    break;
                case "HIDuncloakonclose":
                    Toggle_Uncloak.IsOn = Convert.ToBoolean(value);
                    break;
                case "HIDvibrateonconnect":
                    Toggle_Vibrate.IsOn = Convert.ToBoolean(value);
                    break;
                case "ControllerManagement":
                    Toggle_ControllerManagement.IsOn = Convert.ToBoolean(value);
                    break;
                case "VibrationStrength":
                    SliderStrength.Value = Convert.ToDouble(value);
                    break;
                case "DesktopLayoutEnabled":
                    Toggle_DesktopLayout.IsOn = Convert.ToBoolean(value);
                    break;
                case "SteamControllerMode":
                    cB_SCModeController.SelectedIndex = Convert.ToInt32(value);
                    ControllerRefresh();
                    break;
                case "HIDmode":
                    cB_HidMode.SelectedIndex = Convert.ToInt32(value);
                    break;
                case "HIDstatus":
                    cB_ServiceSwitch.SelectedIndex = Convert.ToInt32(value);
                    break;
            }
        });
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
    }

    public void Page_Closed()
    {
    }

    private void ControllerUnplugged(IController Controller, bool IsPowerCycling, bool WasTarget)
    {
        // UI thread (async)
        Application.Current.Dispatcher.Invoke(() =>
        {
            SimpleStackPanel targetPanel = Controller.IsVirtual() ? VirtualDevicesList : PhysicalDevicesList;

            // Search for an existing controller, remove it
            foreach (IController ctrl in targetPanel.Children)
            {
                if (ctrl.GetContainerInstancePath() == Controller.GetContainerInstancePath())
                {
                    if (!IsPowerCycling)
                    {
                        targetPanel.Children.Remove(ctrl);
                        break;
                    }
                }
            }

            ControllerRefresh();
        });
    }

    private void ControllerPlugged(IController Controller, bool IsPowerCycling)
    {
        // UI thread (async)
        Application.Current.Dispatcher.Invoke(() =>
        {
            SimpleStackPanel targetPanel = Controller.IsVirtual() ? VirtualDevicesList : PhysicalDevicesList;

            // Search for an existing controller, remove it
            foreach (IController ctrl in targetPanel.Children)
                if (ctrl.GetContainerInstancePath() == Controller.GetContainerInstancePath())
                    return;

            // Add new controller to list if no existing controller was found
            targetPanel.Children.Add(Controller);

            // todo: move me
            Button ui_button_hook = Controller.GetButtonHook();
            ui_button_hook.Click += (sender, e) => ControllerHookClicked(Controller);

            Button ui_button_hide = Controller.GetButtonHide();
            ui_button_hide.Click += (sender, e) => ControllerHideClicked(Controller);

            ControllerRefresh();
        });
    }

    private void ControllerManager_ControllerSelected(IController Controller)
    {
        // UI thread (async)
        ControllerRefresh();
    }

    private Dialog dialog = new Dialog(MainWindow.GetCurrent())
    {
        Title = Properties.Resources.ControllerPage_ControllerManagement,
        Content = Properties.Resources.ControllerPage_ControllerManagement_Content,
        CanClose = false
    };

    private void ControllerManager_Working(ControllerManagerStatus status, int attempts)
    {
        // UI thread (async)
        Application.Current.Dispatcher.Invoke(async () =>
        {
            switch (status)
            {
                case ControllerManagerStatus.Busy:
                    {
                        switch (attempts)
                        {
                            case 0:
                                // set dialog settings
                                dialog.CanClose = false;
                                dialog.DefaultButton = ContentDialogButton.Primary;
                                dialog.CloseButtonText = string.Empty;
                                dialog.PrimaryButtonText = string.Empty;

                                dialog.Content = Properties.Resources.ControllerPage_ControllerManagment_Attempting;
                                break;
                            case 1:
                                dialog.Content = Properties.Resources.ControllerPage_ControllerManagment_Reordering;
                                break;
                            case 2:
                                dialog.Content = Properties.Resources.ControllerPage_ControllerManagment_RedOrGreen;
                                break;
                            case 3:
                                dialog.Content = Properties.Resources.ControllerPage_ControllerManagment_FinalAttempt;
                                break;
                        }

                        dialog.Show();
                    }
                    break;

                case ControllerManagerStatus.Succeeded:
                    {
                        dialog.UpdateContent(Properties.Resources.ControllerPage_ControllerManagment_Done);
                        await Task.Delay(2000);
                        dialog.Hide();
                    }
                    break;

                case ControllerManagerStatus.Failed:
                    {
                        // set dialog settings
                        dialog.CanClose = true;
                        dialog.DefaultButton = ContentDialogButton.Close;
                        dialog.CloseButtonText = Properties.Resources.ControllerPage_Close;
                        dialog.PrimaryButtonText = Properties.Resources.ControllerPage_TryAgain;

                        dialog.Content = Properties.Resources.ControllerPage_ControllerManagment_Failed;

                        Task<ContentDialogResult> dialogTask = dialog.ShowAsync();

                        await dialogTask; // sync call

                        switch (dialogTask.Result)
                        {
                            case ContentDialogResult.None:
                                Toggle_ControllerManagement.IsOn = true;
                                break;
                        }

                        dialog.Hide();
                    }
                    break;
            }

            // here ?
            ControllerRefresh();
        });
    }

    private void VirtualManager_ControllerSelected(HIDmode mode)
    {
        return;

        // UI thread (async)
        Application.Current.Dispatcher.Invoke(() =>
        {
            cB_HidMode.SelectedIndex = (int)mode;
        });
    }

    private void ControllerHookClicked(IController Controller)
    {
        if (Controller.IsBusy)
            return;

        var path = Controller.GetContainerInstancePath();
        ControllerManager.SetTargetController(path, false);

        ControllerRefresh();
    }

    private void ControllerHideClicked(IController Controller)
    {
        if (Controller.IsBusy)
            return;

        if (Controller.IsHidden())
            Controller.Unhide();
        else
            Controller.Hide();

        ControllerRefresh();
    }

    private void ControllerRefresh()
    {
        IController targetController = ControllerManager.GetTargetController();
        bool hasPhysical = ControllerManager.HasPhysicalController();
        bool hasVirtual = ControllerManager.HasVirtualController();
        bool hasTarget = targetController != null;

        // UI thread (async)
        Application.Current.Dispatcher.Invoke(() =>
        {
            PhysicalDevices.Visibility = hasPhysical ? Visibility.Visible : Visibility.Collapsed;
            WarningNoPhysical.Visibility = !hasPhysical ? Visibility.Visible : Visibility.Collapsed;

            bool isPlugged = hasPhysical && hasTarget;
            bool isHidden = false;
            if (targetController is not null)
                isHidden = targetController.IsHidden();

            // hint: Has physical controller, but is not connected
            HintsNoPhysicalConnected.Visibility = hasPhysical && !isPlugged ? Visibility.Visible : Visibility.Collapsed;

            // hint: Has physical controller (not Neptune) hidden, but no virtual controller
            VirtualDevices.Visibility = hasVirtual ? Visibility.Visible : Visibility.Collapsed;
            WarningNoVirtual.Visibility = isHidden && !hasVirtual ? Visibility.Visible : Visibility.Collapsed;

            // hint: Has physical controller not hidden, and virtual controller
            bool hasDualInput = isPlugged && !isHidden && hasVirtual;
            HintsNotMuted.Visibility = hasDualInput ? Visibility.Visible : Visibility.Collapsed;

            Hints.Visibility = (HintsNoPhysicalConnected.Visibility == Visibility.Visible ||
                                HintsHIDManagedByProfile.Visibility == Visibility.Visible ||
                                HintsNotMuted.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private async void cB_HidMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cB_HidMode.SelectedIndex == -1)
            return;

        controllerMode = (HIDmode)cB_HidMode.SelectedIndex;

        // only change HIDmode setting if current profile is default or set to default controller
        var currentProfile = ProfileManager.GetCurrent();
        if (currentProfile.Default || currentProfile.HID == HIDmode.NotSelected)
        {
            SettingsManager.SetProperty("HIDmode", cB_HidMode.SelectedIndex);
        }
    }

    private void cB_ServiceSwitch_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cB_HidMode.SelectedIndex == -1)
            return;

        controllerStatus = (HIDstatus)cB_ServiceSwitch.SelectedIndex;

        SettingsManager.SetProperty("HIDstatus", cB_ServiceSwitch.SelectedIndex);
    }

    private void Toggle_Cloaked_Toggled(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SettingsManager.SetProperty("HIDcloakonconnect", Toggle_Cloaked.IsOn);
    }

    private void Toggle_Uncloak_Toggled(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SettingsManager.SetProperty("HIDuncloakonclose", Toggle_Uncloak.IsOn);
    }

    private void SliderStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var value = SliderStrength.Value;
        if (double.IsNaN(value))
            return;

        SliderStrength.Value = value;

        if (!IsLoaded)
            return;

        SettingsManager.SetProperty("VibrationStrength", value);
    }

    private void Toggle_Vibrate_Toggled(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SettingsManager.SetProperty("HIDvibrateonconnect", Toggle_Vibrate.IsOn);
    }

    private void Toggle_ControllerManagement_Toggled(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SettingsManager.SetProperty("ControllerManagement", Toggle_ControllerManagement.IsOn);
    }

    private void Button_Layout_Click(object sender, RoutedEventArgs e)
    {
        // prepare layout editor, desktopLayout gets saved automatically
        LayoutTemplate desktopTemplate = new(LayoutManager.GetDesktop())
        {
            Name = LayoutTemplate.DesktopLayout.Name,
            Description = LayoutTemplate.DesktopLayout.Description,
            Author = Environment.UserName,
            Executable = string.Empty,
            Product = string.Empty // UI might've set something here, nullify
        };
        MainWindow.layoutPage.UpdateLayoutTemplate(desktopTemplate);
        MainWindow.NavView_Navigate(MainWindow.layoutPage);
    }

    private void Toggle_DesktopLayout_Toggled(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        // temporary settings
        SettingsManager.SetProperty("DesktopLayoutEnabled", Toggle_DesktopLayout.IsOn, false, true);
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        ((Expander)sender).BringIntoView();
    }

    private void cB_SCModeController_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        SettingsManager.SetProperty("SteamControllerMode", Convert.ToBoolean(cB_SCModeController.SelectedIndex));
    }
}