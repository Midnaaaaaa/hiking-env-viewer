import argparse
import json
from PIL import Image

def process_file(input_path, output_path, heightscale, imagepath, numberbits, meterspixel):

    heightmap = Image.open(imagepath)

    with open(input_path, 'r') as f:
        data = json.load(f)

    for plant in data['plants']:
        x = round(plant['pos']['x'])
        z = round(plant['pos']['z'])

        if 0 <= x < heightmap.width * meterspixel and 0 <= z < heightmap.height * meterspixel:
            height = heightmap.getpixel((x // meterspixel, z // meterspixel))

            # Actualizar la altura de la planta
            plant['pos']['y'] = height * heightscale / (2**numberbits) #numberbits es para normalizar
            plant['pos']['x'] = plant['pos']['x']
            plant['pos']['z'] = heightmap.height * meterspixel - plant['pos']['z']
        else:
            # Descartar la planta si está fuera de los límites del mapa de altura
            data['plants'].remove(plant)

    # Guardar los datos JSON modificados
    with open(output_path + ".json", 'w') as f:
        json.dump(data, f)

def main():

    parser = argparse.ArgumentParser(description='This script processes the paths')

    # Agregar los argumentos
    parser.add_argument('-i', '--input', type=str, help='Input file path', required=True)
    parser.add_argument('-o','--output', type=str, help='Output file path')
    parser.add_argument('-he','--heightScale', type=int, help='Heightscale', required=True)
    parser.add_argument('-ii','--inputimage', type=str, help='Path of the heightscale image', required=True)
    parser.add_argument('-nb','--numberbits', type=int, help='Number of bits', required=True)
    parser.add_argument('-mp','--meterspixel', type=int, help='Number of bits', required=True)

    # Parsear los argumentos
    args = parser.parse_args()
    if args.output == None: args.output = "output.txt"
    process_file(args.input, args.output, args.heightScale, args.inputimage, args.numberbits, args.meterspixel)


main()
