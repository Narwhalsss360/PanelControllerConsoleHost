# Console Host

# Commands
*How to read:* CommandName(argument-type argument-name)
The argument type tells you what kind of values should be put in in that position.

## Show(ShowOptions option)
### ShowOptions switch
**Values**
- All
    - Shows everything
- LoadedExtensions
    - Shows loaded extensions by their name and type full name (type full name for all commands)
- Profiles
    - Shows all profiles
- Mappings
    - Shows all mappings of *currently selected* profile
- Panels
    - Shows all panels
- Properties
    - Shows all properties *currently selected* object
- Selected
    - Shows what is current selected

## Create(CreateOptions option, String name, String[] flags)

## Select(SelectOptions option, String value)

## Deselect()

## Edit(EditOptions option, String value)

## Remove()

## LogDump(String format)

## Clear()

## SaveAll(Object sender, EventArgs args)

## Quit()