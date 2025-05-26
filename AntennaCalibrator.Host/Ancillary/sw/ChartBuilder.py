# Importing the required module
import matplotlib.pyplot as plt
import os
import sys

path = sys.argv[1]

if (os.path.exists(path)):
    #Read file with fitness values 
    f = open(path)
    content = f.readlines()

    x = []  # x axis values
    y = []  # y axis values

    generations = 1

    for value in content:
        x.append(generations)
        y.append(float(value))
        generations = generations + 1
      
    # Plotting the points 
    plt.plot(x, y)
      
    # Naming the x axis
    plt.xlabel('Generations')
    # Naming the y axis
    plt.ylabel('Fitness')
      
    # Giving a title to my graph
    plt.title('Fitness trends over generations')
      
    # Add grid to plot
    plt.grid()
    
    # Function to show the plot
    plt.show()