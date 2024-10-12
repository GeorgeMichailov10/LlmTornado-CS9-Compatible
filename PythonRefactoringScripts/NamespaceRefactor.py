import os
import re

def convert_namespace(file_path):
    with open(file_path, 'r', encoding='utf-8') as file:
        lines = file.readlines()

    namespace_pattern = re.compile(r'^\s*namespace\s+(\w+);')
    new_lines = []
    namespace_found = False

    for line in lines:
        match = namespace_pattern.match(line)
        if match and not namespace_found:
            namespace_name = match.group(1)
            new_lines.append(f'namespace {namespace_name}\n{{\n')
            namespace_found = True
        else:
            new_lines.append(line)

    if namespace_found:
        # Ensure the file ends with a closing brace
        if not new_lines[-1].strip().endswith('}'):
            new_lines.append('}\n')
        with open(file_path, 'w', encoding='utf-8') as file:
            file.writelines(new_lines)

def process_directory(directory):
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                convert_namespace(file_path)

def convert_single_namespace(file_path):
    convert_namespace(file_path)

if __name__ == '__main__':
    # target_directory = '.'  
    # Replace with the target directory path
    # process_directory(target_directory)
    # Example usage for converting a single namespace
    single_file_path = os.path.join(os.getcwd(), 'LlmTornado/Assistants/AssistantFileResponse.cs')
    convert_single_namespace(single_file_path)
