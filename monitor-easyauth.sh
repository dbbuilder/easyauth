#!/bin/bash

# EasyAuth Framework Version Monitor Script
# Checks NuGet and GitHub every minute for v2.3.0 updates

LOG_FILE="/mnt/d/dev2/easyauth/monitor.log"
LAST_VERSION_FILE="/mnt/d/dev2/easyauth/last_version.txt"

log_message() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

check_nuget_version() {
    log_message "Checking NuGet for EasyAuth.Framework updates..."
    
    # Check NuGet API for available versions
    local response=$(curl -s "https://api.nuget.org/v3-flatcontainer/easyauth.framework/index.json")
    local latest_version=$(echo "$response" | jq -r '.versions | .[-1]' 2>/dev/null)
    
    if [ "$latest_version" != "null" ] && [ "$latest_version" != "" ]; then
        log_message "Latest NuGet version: $latest_version"
        
        # Check if v2.3.0 or newer is available
        if [[ "$latest_version" =~ ^2\.[3-9]\.[0-9]+$ ]] || [[ "$latest_version" =~ ^[3-9]\.[0-9]+\.[0-9]+$ ]]; then
            log_message "🎉 NEW VERSION FOUND: $latest_version is available on NuGet!"
            echo "$latest_version" > "$LAST_VERSION_FILE"
            return 0
        fi
    else
        log_message "Could not retrieve version information from NuGet"
    fi
    
    return 1
}

check_github_releases() {
    log_message "Checking GitHub releases for EasyAuth Framework..."
    
    # Check GitHub API for latest release
    local response=$(curl -s "https://api.github.com/repos/dbbuilder/easyauth/releases/latest")
    local tag_name=$(echo "$response" | jq -r '.tag_name' 2>/dev/null)
    local published_at=$(echo "$response" | jq -r '.published_at' 2>/dev/null)
    
    if [ "$tag_name" != "null" ] && [ "$tag_name" != "" ]; then
        log_message "Latest GitHub release: $tag_name (published: $published_at)"
        
        # Extract version number (remove 'v' prefix if present)
        local version=${tag_name#v}
        
        # Check if v2.3.0 or newer
        if [[ "$version" =~ ^2\.[3-9]\.[0-9]+$ ]] || [[ "$version" =~ ^[3-9]\.[0-9]+\.[0-9]+$ ]]; then
            log_message "🎉 NEW GITHUB RELEASE: $version is available!"
            
            # Get release notes
            local body=$(echo "$response" | jq -r '.body' 2>/dev/null)
            log_message "Release Notes:"
            echo "$body" | tee -a "$LOG_FILE"
            
            return 0
        fi
    else
        log_message "Could not retrieve GitHub release information"
    fi
    
    return 1
}

check_github_commits() {
    log_message "Checking GitHub commits for recent changes..."
    
    # Check recent commits
    local response=$(curl -s "https://api.github.com/repos/dbbuilder/easyauth/commits?per_page=5")
    local commit_count=$(echo "$response" | jq length 2>/dev/null)
    
    if [ "$commit_count" -gt 0 ]; then
        log_message "Recent commits found:"
        echo "$response" | jq -r '.[] | "- \(.commit.message | split("\n")[0]) (\(.commit.author.date))"' | tee -a "$LOG_FILE"
    fi
}

main() {
    log_message "=== EasyAuth Framework Monitor Started ==="
    
    # Check NuGet
    if check_nuget_version; then
        log_message "✅ Action needed: Update packages to latest version"
    fi
    
    # Check GitHub releases
    if check_github_releases; then
        log_message "✅ Action needed: Review release notes and implement changes"
    fi
    
    # Check recent commits for development activity
    check_github_commits
    
    log_message "=== Monitor Check Complete ==="
    echo "" >> "$LOG_FILE"
}

# Run the main function
main