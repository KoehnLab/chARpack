using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace chARpack
{
    public class TransitionInteractionTypeFlagEnumDropdown : TMP_Dropdown
    {
        public TransitionManager.InteractionType selectedOptions;
        private List<TMP_Dropdown.OptionData> optionDataList;

        protected override void Start()
        {
            base.Start();
            SetupDropdown();
            //UpdateDropdownCaption();
            RefreshDropdown();
        }

        private void SetupDropdown()
        {
            ClearOptions();
            optionDataList = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("All"),
            new TMP_Dropdown.OptionData("None")
        };

            foreach (TransitionManager.InteractionType option in Enum.GetValues(typeof(TransitionManager.InteractionType)))
            {
                if (option == TransitionManager.InteractionType.NONE || option == TransitionManager.InteractionType.ALL) continue;
                optionDataList.Add(new TMP_Dropdown.OptionData(option.ToString()));
            }

            AddOptions(optionDataList);
            // Add a listener to handle dropdown item selection
            onValueChanged.AddListener(OnDropdownValueChanged);
        }

        private void OnDropdownValueChanged(int index)
        {
            // Determine which item was selected
            if (index == 0) // "Select All"
            {
                SelectAll();
            }
            else if (index == 1) // "Select None"
            {
                SelectNone();
            }
            else
            {
                OnOptionToggle(index);
            }
        }

        public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            // Handle click events, similar to the regular Dropdown
            RefreshDropdown();
        }

        public void OnOptionToggle(int index)
        {
            if (index == 0) // "Select All"
            {
                SelectAll();
            }
            else if (index == 1) // "Select None"
            {
                SelectNone();
            }
            else
            {
                var option = (TransitionManager.InteractionType)(1 << (index - 2)); // Adjust index and cast
                int selectedInt = Convert.ToInt32(selectedOptions);
                int optionInt = Convert.ToInt32(option);

                if ((selectedInt & optionInt) == optionInt)
                {
                    selectedInt &= ~optionInt; // Remove the option
                }
                else
                {
                    selectedInt |= optionInt; // Add the option
                }

                selectedOptions = (TransitionManager.InteractionType)selectedInt;
            }
            RefreshDropdown();
            UpdateDropdownCaption();
        }


        private void UpdateDropdownCaption()
        {
            // Here, we assume that TMP_Text is used instead of the regular Text component
            if (selectedOptions == TransitionManager.InteractionType.NONE)
            {
                captionText.text = "None";
            }
            else if (selectedOptions == TransitionManager.InteractionType.ALL)
            {
                captionText.text = "All";
            }
            else
            {
                captionText.text = selectedOptions.ToString();
            }
        }

        private void RefreshDropdown()
        {
            // Iterate through all dropdown items and set checkmarks based on current selection
            for (int i = 2; i < optionDataList.Count; i++) // Start at index 2 to skip "All" and "None"
            {
                var option = (TransitionManager.InteractionType)(1 << (i - 2));
                var item = template.GetComponentInChildren<Toggle>();

                if ((selectedOptions & option) == option)
                {
                    item.isOn = true;
                    item.Select();
                }
                else
                {
                    item.isOn = false;
                }
            }
        }

        public void SelectAll()
        {
            selectedOptions = TransitionManager.InteractionType.ALL;
            RefreshDropdown();
            UpdateDropdownCaption();
        }

        public void SelectNone()
        {
            selectedOptions = TransitionManager.InteractionType.NONE;
            RefreshDropdown();
            UpdateDropdownCaption();
        }
    }
}
