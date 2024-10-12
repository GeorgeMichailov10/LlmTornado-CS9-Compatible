import os
import re

def find_files_with_required_property(directory):
    print(f"Scanning directory: {directory}")
    files_with_required = []
    # Pattern to match lines like: public required string ToolCallId { get; }
    pattern = r'public\s+required\s+\w+\s+\w+\s*{[^}]*}'

    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                if re.search(pattern, content):
                    files_with_required.append(file_path)
                    print(f"Required property found in: {file_path}")
                else:
                    print(f"No required properties in: {file_path}")

    return files_with_required

if __name__ == "__main__":
    # Get the parent directory of the script
    script_dir = os.path.dirname(os.path.abspath(__file__))
    parent_dir = os.path.dirname(script_dir)
    
    # Get all directories in the parent directory, excluding the script's directory
    project_directories = [
        d for d in os.listdir(parent_dir)
        if os.path.isdir(os.path.join(parent_dir, d)) and d != os.path.basename(script_dir)
    ]
    
    print("Scanning the following directories:")
    for dir_name in project_directories:
        print(f"- {dir_name}")
    
    all_files_with_required = []
    
    # Scan each project directory for required properties
    for dir_name in project_directories:
        directory_path = os.path.join(parent_dir, dir_name)
        files_found = find_files_with_required_property(directory_path)
        all_files_with_required.extend(files_found)
    
    print("\nFiles containing required properties:")
    for file_path in all_files_with_required:
        print(file_path)
    
    print("\nScanning complete.")