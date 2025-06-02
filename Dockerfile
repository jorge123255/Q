FROM mono:latest

# Install necessary packages
RUN apt-get update && apt-get install -y \
    nuget \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /src

# The actual build will be done via the entrypoint script
# Source code is mounted at runtime via -v flag
ENTRYPOINT ["/bin/bash", "/src/docker-build.sh"]
