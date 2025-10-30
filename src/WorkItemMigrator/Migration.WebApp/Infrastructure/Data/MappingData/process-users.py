import csv
import re

def normalize_name_to_kebab(name):
    if not name or name.strip() == '':
        return ''
    
    # Remove special characters and normalize
    name = re.sub(r'[^\w\s\-\.]', '', name)
    # Replace spaces, dots, and underscores with hyphens
    name = re.sub(r'[\s\._@]+', '-', name)
    # Convert to lowercase
    name = name.lower()
    # Remove multiple consecutive hyphens
    name = re.sub(r'-+', '-', name)
    # Remove leading/trailing hyphens
    name = name.strip('-')
    
    return name

def is_app_user(name):
    app_indicators = [
        'addon', 'app', 'integration', 'bot', 'service', 'system', 
        'automation', 'connector', 'plugin', 'cloud', 'pipelines',
        'github', 'gitlab', 'jira', 'confluence', 'atlassian',
        'microsoft', 'teams', 'slack', 'trello', 'bitbucket',
        'former user', 'build_service', 'jenkins', 'opsgenie'
    ]
    
    name_lower = name.lower()
    return any(indicator in name_lower for indicator in app_indicators)

# Read original file and process
with open('initial-user-mapping.csv', 'r', encoding='utf-8') as infile:
    reader = csv.DictReader(infile)
    
    with open('updated-user-mapping.csv', 'w', newline='', encoding='utf-8') as outfile:
        fieldnames = ['JiraUserId', 'JiraFirstLastName', 'JiraUserEmail', 'AzureDevOpsUser', 'AppUser']
        writer = csv.DictWriter(outfile, fieldnames=fieldnames)
        writer.writeheader()
        
        for row in reader:
            user_id = row['JiraUserId'].strip()
            name = row['JiraFirstLastName'].strip()
            email = row['JiraUserEmail'].strip()
            
            # Determine AzureDevOpsUser
            if email:
                # Use email prefix as Azure user
                azure_user = email.split('@')[0].replace('.', '.')
                if '.' in azure_user:
                    azure_user = '.'.join(word.capitalize() for word in azure_user.split('.'))
                else:
                    azure_user = azure_user.capitalize()
            else:
                # Use kebab-case name
                azure_user = normalize_name_to_kebab(name)
            
            # Determine if app user
            app_user = 1 if is_app_user(name) or not email else 0
            
            writer.writerow({
                'JiraUserId': user_id,
                'JiraFirstLastName': name,
                'JiraUserEmail': email,
                'AzureDevOpsUser': azure_user,
                'AppUser': app_user
            })

print("Processing complete!")