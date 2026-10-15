// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// Lazer-Kai's settings all at once: the recommended ones, or a set a mapper saved. There are enough of them that
    /// somebody starting out has no way of knowing where to begin (2026-09-25).
    /// </summary>
    public partial class SettingsPresetSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.SettingsPresetHeader;

        /// <summary>
        /// The choices that are not a saved preset. Neither can be a preset's name, which never starts with a colon.
        /// </summary>
        public const string RECOMMENDED = @":recommended";

        public const string BEFORE_APPLYING = @":before-applying";

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private SettingsOverlay? settings { get; set; }

        private IBindable<Visibility>? settingsState;

        private LazerKaiSettingsPresetStore store = null!;

        private readonly Bindable<string> selected = new Bindable<string>(RECOMMENDED);
        private readonly Bindable<string> name = new Bindable<string>(string.Empty);

        private PresetDropdown dropdown = null!;
        private SettingsButton save = null!;
        private SettingsButton delete = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            store = new LazerKaiSettingsPresetStore(storage);

            Children = new Drawable[]
            {
                new SettingsItemV2(dropdown = new PresetDropdown
                {
                    Caption = LazerKaiSettingsStrings.SettingsPreset,
                    HintText = LazerKaiSettingsStrings.SettingsPresetDescription,
                    Current = selected,
                }),
                new SettingsButton
                {
                    Text = LazerKaiSettingsStrings.ApplySettingsPreset,
                    Action = askToApply,
                },
                new SettingsTextBox
                {
                    // saving is apart from applying, and the name would otherwise sit right under the button above it.
                    Margin = new MarginPadding { Top = BUTTON_GAP * 2 },
                    LabelText = LazerKaiSettingsStrings.SettingsPresetName,
                    Current = name,
                },
                save = new SettingsButton
                {
                    Text = LazerKaiSettingsStrings.SaveSettingsPreset,
                    Action = saveCurrent,
                },
                delete = new DangerousSettingsButton
                {
                    Margin = new MarginPadding { Top = BUTTON_GAP },
                    Text = LazerKaiSettingsStrings.DeleteSettingsPreset,
                    Action = askToDelete,
                },
                new SettingsButton
                {
                    Margin = new MarginPadding { Top = BUTTON_GAP },
                    Text = LazerKaiSettingsStrings.OpenSettingsPresetFolder,
                    Action = () => storage.GetStorageForDirectory(LazerKaiSettingsPresetStore.DIRECTORY).PresentExternally(),
                },
            };

            refreshItems();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            name.BindValueChanged(n => save.Enabled.Value = LazerKaiSettingsPresetStore.NameFor(n.NewValue).Length > 0, true);
            selected.BindValueChanged(s => delete.Enabled.Value = !s.NewValue.StartsWith(':'), true);

            // read again each time the settings are opened: the welcome after the first-run setup applies the
            // recommended settings from outside them, and the settings put aside then would otherwise not be on the
            // list to go back to until the next start.
            settingsState = settings?.State.GetBoundCopy();
            settingsState?.BindValueChanged(s =>
            {
                if (s.NewValue == Visibility.Visible)
                    refreshItems();
            });
        }

        private void refreshItems()
        {
            var items = new[] { RECOMMENDED }.AsEnumerable();

            if (store.LoadBeforeApplying() != null)
                items = items.Append(BEFORE_APPLYING);

            var list = items.Concat(store.Names()).ToList();

            // a preset taken out of the folder by hand since the list was last read.
            if (!list.Contains(selected.Value))
                selected.Value = RECOMMENDED;

            dropdown.Items = list;
        }

        private LazerKaiSettingsPreset? presetFor(string choice)
        {
            switch (choice)
            {
                case RECOMMENDED:
                    return new LazerKaiSettingsPreset(LazerKaiSettingsPreset.RECOMMENDED);

                case BEFORE_APPLYING:
                    return store.LoadBeforeApplying();

                default:
                    return store.Load(choice);
            }
        }

        private void askToApply()
        {
            string choice = selected.Value;
            var preset = presetFor(choice);

            if (preset == null)
                return;

            // the recommended settings leave the colours and the skin alone; a preset of the mapper's own has them.
            var body = choice == RECOMMENDED ? LazerKaiSettingsStrings.ApplySettingsPresetBody : LazerKaiSettingsStrings.ApplySavedSettingsPresetBody;
            var dialog = new ApplySettingsPresetDialog(PresetDropdown.NameOf(choice), body, () => apply(choice, preset));

            if (dialogOverlay != null)
                dialogOverlay.Push(dialog);
            else
                apply(choice, preset);
        }

        private void apply(string choice, LazerKaiSettingsPreset preset)
        {
            apply(store, config, notifications, choice, preset);
            refreshItems();
        }

        /// <summary>
        /// The question whether to apply the recommended settings, applying them on a yes, for asking it from outside
        /// these settings: the welcome after the first-run setup (<see cref="FirstRunSetup.LazerKaiWelcomeDialog"/>).
        /// </summary>
        /// <remarks>
        /// Asked and applied the same way as from here, so that what was there before is put aside to go back to.
        /// </remarks>
        public static PopupDialog AskToApplyRecommended(Storage storage, OsuConfigManager config, INotificationOverlay? notifications) =>
            new ApplySettingsPresetDialog(PresetDropdown.NameOf(RECOMMENDED), LazerKaiSettingsStrings.ApplySettingsPresetBody,
                () => apply(new LazerKaiSettingsPresetStore(storage), config, notifications, RECOMMENDED, new LazerKaiSettingsPreset(LazerKaiSettingsPreset.RECOMMENDED)));

        private static void apply(LazerKaiSettingsPresetStore store, OsuConfigManager config, INotificationOverlay? notifications, string choice, LazerKaiSettingsPreset preset)
        {
            // the settings as they were go aside first, so that applying can be undone by applying them.
            store.SaveBeforeApplying(LazerKaiSettingsPreset.Capture(config));
            preset.ApplyTo(config);

            notifications?.Post(new SimpleNotification
            {
                Icon = FontAwesome.Solid.Check,
                Text = LazerKaiSettingsStrings.AppliedSettingsPreset(PresetDropdown.NameOf(choice)),
            });
        }

        private void saveCurrent()
        {
            string presetName = LazerKaiSettingsPresetStore.NameFor(name.Value);

            if (presetName.Length == 0)
                return;

            store.Save(presetName, LazerKaiSettingsPreset.Capture(config));
            refreshItems();
            selected.Value = presetName;
            name.Value = string.Empty;

            notifications?.Post(new SimpleNotification
            {
                Icon = FontAwesome.Solid.Save,
                Text = LazerKaiSettingsStrings.SavedSettingsPreset(presetName),
            });
        }

        private void askToDelete()
        {
            string choice = selected.Value;

            if (choice.StartsWith(':'))
                return;

            dialogOverlay?.Push(new ConfirmDialog(LazerKaiSettingsStrings.DeleteSettingsPresetHeader(choice), () =>
            {
                store.Delete(choice);
                selected.Value = RECOMMENDED;
                refreshItems();
            }));
        }

        private partial class PresetDropdown : FormDropdown<string>
        {
            public static LocalisableString NameOf(string choice)
            {
                switch (choice)
                {
                    case RECOMMENDED:
                        return LazerKaiSettingsStrings.RecommendedSettings;

                    case BEFORE_APPLYING:
                        return LazerKaiSettingsStrings.SettingsBeforeApplying;

                    default:
                        return choice;
                }
            }

            protected override LocalisableString GenerateItemText(string item) => NameOf(item);
        }

        public partial class ApplySettingsPresetDialog : PopupDialog
        {
            public ApplySettingsPresetDialog(LocalisableString presetName, LocalisableString body, Action apply)
            {
                HeaderText = LazerKaiSettingsStrings.ApplySettingsPresetHeader(presetName);
                BodyText = body;

                Icon = FontAwesome.Solid.ExclamationTriangle;

                Buttons = new PopupDialogButton[]
                {
                    new PopupDialogOkButton
                    {
                        Text = LazerKaiSettingsStrings.ApplySettingsPresetConfirm,
                        Action = apply,
                    },
                    new PopupDialogCancelButton
                    {
                        Text = WebCommonStrings.ButtonsCancel,
                    },
                };
            }
        }
    }
}
