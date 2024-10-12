import os
import re

def refactor_namespace(directory):
    print(f"Processing directory: {directory}")
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()

                # Search for the C# 10 namespace syntax
                pattern = r'^namespace\s+([^;]+);'
                match = re.search(pattern, content, re.MULTILINE)

                if match:
                    # Replace with C# 9 namespace syntax
                    new_content = re.sub(pattern, r'namespace \1\n{', content, count=1, flags=re.MULTILINE)
                    new_content += '\n}'  # Add closing bracket at the end of the file

                    # Write the modified content back to the file
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(new_content)
                    print(f"Refactored: {file_path}")
                else:
                    print(f"No namespace declaration found in: {file_path}")

if __name__ == "__main__":
    # Get the parent directory of the script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    parent_dir = os.path.dirname(script_dir)
    
    # Get all directories in the parent directory, excluding the script's directory
    project_directories = [d for d in os.listdir(parent_dir) if os.path.isdir(os.path.join(parent_dir, d)) and d != os.path.basename(script_dir)]
    
    print("Processing the following directories:")
    for dir_name in project_directories:
        print(f"- {dir_name}")
    
    # Process each directory
    for dir_name in project_directories:
        directory_path = os.path.join(parent_dir, dir_name)
        refactor_namespace(directory_path)

    print("Refactoring complete.")
